// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Orleans;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCore.SignalR.Orleans.Tests
{
    public partial class OrleansHubLifetimeManagerTests
    {
        [Fact]
        public async Task SendAllAsyncWritesToAllConnectionsOutput()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.SendAllAsync("Hello", new object[] { "World" }).OrTimeout();

                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);

                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task SendAllAsyncDoesNotWriteToDisconnectedConnectionsOutput()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.OnDisconnectedAsync(connection2).OrTimeout();

                await manager.SendAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);
                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task SendGroupAsyncWritesToAllConnectionsInGroupOutput()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.AddToGroupAsync(connection1.ConnectionId, "group").OrTimeout();

                await manager.SendGroupAsync("group", "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);
                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task SendGroupExceptAsyncDoesNotWriteToExcludedConnections()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.AddToGroupAsync(connection1.ConnectionId, "group").OrTimeout();
                await manager.AddToGroupAsync(connection2.ConnectionId, "group").OrTimeout();

                await manager.SendGroupExceptAsync("group", "Hello", new object[] { "World" }, new[] { connection2.ConnectionId }).OrTimeout();

                await AssertMessageAsync(client1);
                Assert.Null(client2.TryRead());
            }
        }

        // ADDED: SendGroupsAsync
        [Fact]
        public async Task SendGroupAsyncWritesToAllConnectionsInGroupsOutput()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.AddToGroupAsync(connection1.ConnectionId, "group").OrTimeout();
                await manager.AddToGroupAsync(connection2.ConnectionId, "group2").OrTimeout();

                await manager.SendGroupsAsync(new string[] { "group", "group2" }, "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task SendConnectionAsyncWritesToConnectionOutput()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);
            }
        }

        // ADDED: SendConnectionsAsync
        [Fact]
        public async Task SendConnectionAsyncWritesToConnectionsOutput()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.SendConnectionsAsync(new string[] { connection1.ConnectionId, connection2.ConnectionId }, "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task DisconnectConnectionRemovesConnectionFromGroup()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.AddToGroupAsync(connection.ConnectionId, "group").OrTimeout();

                await manager.OnDisconnectedAsync(connection).OrTimeout();

                await manager.SendGroupAsync("group", "Hello", new object[] { "World" }).OrTimeout();

                Assert.Null(client.TryRead());

                // ADDED: GetConnectionIdsAsync
                var result = await _fixture.TestCluster.Client.GetGroup("group", typeof(MyHub).GUID).GetConnectionIdsAsync().OrTimeout();
                Assert.Equal(0, result.Count);
            }
        }

        [Fact]
        public async Task RemoveGroupFromLocalConnectionNotInGroupDoesNothing()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.RemoveFromGroupAsync(connection.ConnectionId, "group").OrTimeout();
            }
        }

        [Fact]
        public async Task AddGroupAsyncForLocalConnectionAlreadyInGroupDoesNothing()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.AddToGroupAsync(connection.ConnectionId, "group").OrTimeout();
                await manager.AddToGroupAsync(connection.ConnectionId, "group").OrTimeout();

                await manager.SendGroupAsync("group", "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);
                Assert.Null(client.TryRead());

                // ADDED: GetConnectionIdsAsync
                var result = await _fixture.TestCluster.Client.GetGroup("group", typeof(MyHub).GUID).GetConnectionIdsAsync().OrTimeout();
                Assert.Equal(1, result.Count);
            }
        }

        [Fact]
        public async Task WritingToGroupWithOneConnectionFailingSecondConnectionStillReceivesMessage()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                // Force an exception when writing to connection
                var connectionMock = HubConnectionContextUtils.CreateMock(client1.Connection);

                var connection1 = connectionMock;
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.AddToGroupAsync(connection1.ConnectionId, "group");
                await manager.OnConnectedAsync(connection2).OrTimeout();
                await manager.AddToGroupAsync(connection2.ConnectionId, "group");

                await manager.SendGroupAsync("group", "Hello", new object[] { "World" }).OrTimeout();
                // connection1 will throw when receiving a group message, we are making sure other connections
                // are not affected by another connection throwing
                await AssertMessageAsync(client2);

                // Repeat to check that group can still be sent to
                await manager.SendGroupAsync("group", "Hello", new object[] { "World" }).OrTimeout();
                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task InvokeUserSendsToAllConnectionsForUser()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client1 = new TestClient(userIdentifier: "userA"))
            using (var client2 = new TestClient(userIdentifier: "userA"))
            using (var client3 = new TestClient(userIdentifier: "userB"))
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);
                var connection3 = HubConnectionContextUtils.Create(client3.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();
                await manager.OnConnectedAsync(connection3).OrTimeout();

                await manager.SendUserAsync("userA", "Hello", new object[] { "World" }).OrTimeout();
                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task StillSubscribedToUserAfterOneOfMultipleConnectionsAssociatedWithUserDisconnects()
        {
            using (var manager = CreateNewHubLifetimeManager<MyHub>())
            using (var client1 = new TestClient(userIdentifier: "userA"))
            using (var client2 = new TestClient(userIdentifier: "userA"))
            using (var client3 = new TestClient(userIdentifier: "userB"))
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);
                var connection3 = HubConnectionContextUtils.Create(client3.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();
                await manager.OnConnectedAsync(connection3).OrTimeout();

                await manager.SendUserAsync("userA", "Hello", new object[] { "World" }).OrTimeout();
                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);

                // Disconnect one connection for the user
                await manager.OnDisconnectedAsync(connection1).OrTimeout();
                await manager.SendUserAsync("userA", "Hello", new object[] { "World" }).OrTimeout();
                await AssertMessageAsync(client2);
            }
        }
    }
}
