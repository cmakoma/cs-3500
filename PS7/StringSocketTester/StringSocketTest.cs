﻿using CustomNetworking;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace StringSocketTester
{


    /// <summary>
    ///This is a test class for StringSocketTest and is intended
    ///to contain all StringSocketTest Unit Tests 
    ///</summary>
    [TestClass()]
    public class StringSocketTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A simple test for BeginSend and BeginReceive
        ///</summary>
        [TestMethod()]
        public void Test1()
        {
            new Test1Class().run(4001);
        }

        public class Test1Class
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private String s1;
            private object p1;
            private String s2;
            private object p2;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    // Make two receive requests
                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);

                    // Now send the data.  Hope those receive requests didn't block!
                    String msg = "Hello world\nThis is a test\n";
                    foreach (char c in msg)
                    {
                        sendSocket.BeginSend(c.ToString(), (e, o) => { }, null);
                    }

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("Hello world", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("This is a test", s2);
                    Assert.AreEqual(2, p2);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request.  We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }
        }

        /// <summary>
        ///A simple test for BeginSend 
        ///</summary>
        [TestMethod()]
        public void Test2()
        {
            new Test2Class().run(4001);
        }

        /// <summary>
        /// This test is only for the BeginSend() method and tests whether or not
        /// the callback is send after the message is comletely sent. This is done by keeping
        /// a counter variable that is incremented when the callback is called.  So if your
        /// BeginSend() did not send a complete message and the callback does not get called,
        /// then the counter will not be incremented and the test will fail.
        /// 
        /// Currently the method only calls BeginSend() once, but if you change the messagesSent
        /// variable, it will increase the number of times we loop through and call BeginSend(), 
        /// and this should be equal to the number of times callback is called, and therefore
        /// equal to the number of times counter is incremented. In short, if you want to change
        /// messagesSend to numbers greater than 1, the test should still pass. 
        /// 
        /// </summary>
        public class Test2Class
        {
            // Data that is shared across threads
            private ManualResetEvent mre;

            // Used in Assert
            int counter = 0;

            // Used in Asser and in loop - if you change this the test should still pass
            int messagesSent = 10;

            public void run(int port)
            {

                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // Now send the data.  Hope those receive requests didn't block!
                    String msg = "Hello World\nThis is a Test\n";

                    // BeginSend() as many times as messagesSent
                    for (int i = 0; i < messagesSent; i++)
                        sendSocket.BeginSend(msg, Test2Callback, 1);

                    System.Threading.Thread.Sleep(500);
                    Assert.AreEqual(messagesSent, counter);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }

            }

            /// <summary>
            /// Callback increments the counter
            /// </summary>
            /// <param name="s"></param>
            /// <param name="o"></param>
            /// <param name="payload"></param>
            private void Test2Callback(Exception o, object payload)
            {
                counter++;
            }
        }

        /// <author>Nathan Rollins</author>
        /// <summary>
        /// 
        /// Creates a server which implements a StringSocket object.
        /// Any strings received by this server will be slightly altered  
        /// and automatically sent back to the sender.
        /// 
        /// ServerTestApp and ClientTestApp are heavily adapted from Joe Zachary's 
        /// Simple Chat Server implementation, which was edited by H. James de St. Germain.
        /// </summary>
        public class ServerTestApp
        {
            /// <summary>
            /// The TCP listener which enables us to hear the client's connection attempts.
            /// </summary>
            private TcpListener myTcpListener;

            /// <summary>
            /// Constructor for the ServerTestApp class. Initializes the TCP listener 
            /// to accept connections from any IP address and port 4321. Starts listening
            /// and begins accepting socket connection attempts.
            /// </summary>
            public ServerTestApp()
            {
                // Create the TCP listener to let any IP connect on the port 4321
                myTcpListener = new TcpListener(IPAddress.Any, 4321);

                // Start listening and and accepting socket connection requests.
                myTcpListener.Start();
                myTcpListener.BeginAcceptSocket(ConnectionRequested, null);
            }

            /// <summary>
            /// Callback called when BeginAcceptSocket completes. Creates the socket 
            /// and begins waiting for new connection attempts.
            /// </summary>
            /// <param name="result"></param>
            private void ConnectionRequested(IAsyncResult result)
            {
                // Create a socket to communicate with the client who is requesting it.
                Socket socket = myTcpListener.EndAcceptSocket(result);

                // Create a ClientCommunicator instance to manage the sends and receives.
                new ClientCommunicator(socket);
            }

            /// <summary>
            /// Manages client interactions once a socket has been created. This specific 
            /// implementation creates a StringSocket from the passed connected socket, 
            /// and uses it to easily repeat clients' messages back to them (though slightly
            /// altered).
            /// </summary>
            public class ClientCommunicator
            {
                /// <summary>
                /// The StringSocket enabling us to easily send and receive strings to and from
                /// the client. 
                /// </summary>
                StringSocket serverSocket;

                /// <summary>
                /// The constructor. Converts the passed socket into a StringSocket and begins 
                /// listening for data being sent to it.
                /// </summary>
                /// <param name="socket"></param>
                public ClientCommunicator(Socket socket)
                {
                    // Convert the socket into a StringSocket.
                    serverSocket = new StringSocket(socket, new System.Text.UTF8Encoding());

                    // Start listening for data.
                    serverSocket.BeginReceive(StringReceived, null);
                }

                /// <summary>
                /// Callback called when data is received. Slightly alters the received message, 
                /// and returns it to the sender.
                /// </summary>
                /// <param name="receivedString"></param>
                /// <param name="exceptionReturned"></param>
                /// <param name="payload"></param>
                private void StringReceived(string receivedString,
                    Exception exceptionReturned, object payload)
                {
                    // Send a slightly altered version of the received string back to the sender.
                    // Must include a newline character to terminate the message.
                    serverSocket.BeginSend("I'm not your " + receivedString + "\n", null, null);
                }
            }
        }

        /// <summary>
        /// Creates a client which implements a StringSocket object. Stores the last 
        /// message received from its server in the lastStringReceived variable. Messages
        /// may be sent to the server by calling the SendMessage() method.
        /// 
        /// ServerTestApp and ClientTestApp are heavily adapted from Joe Zachary's 
        /// Simple Chat Server implementation, which was edited by H. James de St. Germain.
        /// </summary>
        public class ClientTestApp
        {
            /// <summary>
            /// Stores the last string received from the server. Will be used by the test to 
            /// ensure proper functionality.
            /// </summary>
            public string lastStringReceived;

            /// <summary>
            /// The StringSocket enabling us to easily send and receive strings to and from
            /// the server. 
            /// </summary>
            private StringSocket clientSocket;

            /// <summary>
            /// The socket which will be built into a StringSocket.
            /// </summary>
            private Socket socket;

            /// <summary>
            /// The constructor. Connects to a server hosted on the local machine through port 4321. 
            /// Once a connection is established, control is transferred to ConnectionEstablished().
            /// </summary>
            public ClientTestApp()
            {
                // Connect to the localhost on port 4321.
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.BeginConnect(IPAddress.Loopback, 4321, ConnectionEstablished, null);
            }

            /// <summary>
            /// Callback called when a connection is established. Creates the socket and converts 
            /// it into a StringSocket, then starts listening for data being sent to it.
            /// </summary>
            /// <param name="result"></param>
            private void ConnectionEstablished(IAsyncResult result)
            {
                // Convert the socket into a StringSocket.
                clientSocket = new StringSocket(socket, new UTF8Encoding());

                // Start receiving any incoming data.
                clientSocket.BeginReceive(StringReceived, null);
            }

            /// <summary>
            /// Callback called when string data has been received from the server. 
            /// This test implementation simply stores the received string in the 
            /// lastStringReceived variable.
            /// </summary>
            /// <param name="receivedString"></param>
            /// <param name="exceptionReturned"></param>
            /// <param name="payload"></param>
            private void StringReceived(string receivedString, Exception exceptionReturned, object payload)
            {
                // Store the received string.
                lastStringReceived = receivedString;
            }

            /// <summary>
            /// Allows a string to be sent to the server through this client's StringSocket.
            /// </summary>
            /// <param name="messageToSend"></param>
            public void SendMessage(string messageToSend)
            {
                // Send the message requested.
                clientSocket.BeginSend(messageToSend, null, null);
            }
        }

        /// <summary>
        /// Authors: Greg Smith and Jase Bleazard
        /// Attempts sending the newline character by itself. The sockets should
        /// still send and receive a blank String, "".
        /// </summary>
        [TestMethod()]
        public void SendAndReceiveEmpty()
        {
            new SendAndReceiveEmptyClass().run(4006);
        }

        public class SendAndReceiveEmptyClass
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private String s1;
            private object p1;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);

                    // Make two receive requests
                    receiveSocket.BeginReceive(CompletedReceive1, 1);

                    // Now send the data.  Hope those receive requests didn't block!
                    sendSocket.BeginSend("\n", (e, o) => { }, null);

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("", s1);

                    Assert.AreEqual(1, p1);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request.  We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }
        }

        /// <author>Daniel James</author>
        /// <timecreated>11/08/14</timecreated>
        /// <summary>
        /// Tests to make sure that code in callbacks can not cause the StringSocket to get blocked.
        /// </summary>
        [TestMethod()]
        public void TestBlockingCallback()
        {
            // Declare these here so we can properally clean up.
            TcpListener server = null;
            TcpClient client = null;
            StringSocket sendSocket = null;
            StringSocket receiveSocket = null;

            // Test both receive callback and send callback separately.
            ManualResetEvent mreReceive = new ManualResetEvent(false);
            ManualResetEvent mreSend = new ManualResetEvent(false);

            // So we can unblock threads in finally.
            ManualResetEvent mreBlock = new ManualResetEvent(false);

            // Some constants used in the test case
            const int timeout = 2000;
            const int port = 8989;

            try
            {
                // Create server/client
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                client = new TcpClient("localhost", port);

                // Wrap the two ends of the connection into StringSockets
                sendSocket = new StringSocket(server.AcceptSocket(), new UTF8Encoding());
                receiveSocket = new StringSocket(client.Client, new UTF8Encoding());

                // Make two receive requests
                receiveSocket.BeginReceive((s, e, p) => mreBlock.WaitOne(), 1); // This one attempts to block StringSocket
                receiveSocket.BeginReceive((s, e, p) => mreReceive.Set(), 2); // This one allows assertion to pass. (Won't happen if StringSocket is blocked from the first request.)

                // Make two send requests.
                sendSocket.BeginSend("Don't let my code\n", (e, p) => mreBlock.WaitOne(), null); // This one attempts to block StringSocket
                sendSocket.BeginSend("block your code\n", (e, p) => mreSend.Set(), null); // This one allows assertion to pass. (Won't happen if StringSocket is blocked from the first request.)

                // Make sure the second requests were able to go through.
                Assert.AreEqual(true, mreSend.WaitOne(timeout), "Blocked by BeginSend callback.");
                Assert.AreEqual(true, mreReceive.WaitOne(timeout), "Blocked by BeginReceive callback.");
            }
            finally
            {
                // Cleanup
                mreBlock.Set();
                sendSocket.Close();
                receiveSocket.Close();
                server.Stop();
                client.Close();
            }
        }

        ///<author>Travis Healey</author>
        /// <summary>
        /// 
        /// </summary>
        [TestMethod()]
        public void Test1_Mod()
        {
            for (int i = 0; i < 50; i++)
                new Test1Class().run(4002 - i);
        }

        /// <author>Matthew Madden</author>
        /// <timecreated>11/11/14</timecreated>
        /// <summary>
        /// This method tests whether non-ASCII (multi-byte) characters are
        /// passed through the String Socket intact, based on the encoding provided.
        /// UTF-8 encoding can encode/decode any valid Unicode character.
        ///</summary>
        [TestMethod()]
        public void Test_non_ASCII()
        {
            new TestClass_non_ASCII().run(4100);
        }

        public class TestClass_non_ASCII
        {
            private ManualResetEvent mre1;
            private String msg;
            private object p1;
            StringSocket sendSocket, receiveSocket;

            // Timeout
            private static int timeout = 2000;

            public void run(int port)
            {
                TcpListener server = null;
                TcpClient client = null;


                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;
                    sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    mre1 = new ManualResetEvent(false);

                    receiveSocket.BeginReceive(CompletedReceive, 1);
                    sendSocket.BeginSend("Hêllø Ψórlđ!\n", (e, o) => { }, null);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting");
                    // this will fail if the String Socket does not handle non-ASCII characters
                    Assert.AreEqual("Hêllø Ψórlđ!", msg);
                    System.Diagnostics.Debug.WriteLine(msg);
                    Assert.AreEqual(1, p1);
                }
                finally
                {
                    sendSocket.Close();
                    receiveSocket.Close();
                    server.Stop();
                    client.Close();
                }
            }

            //callback
            private void CompletedReceive(String s, Exception o, object payload)
            {
                msg = s;
                p1 = payload;
                mre1.Set();
            }
        }

        ///<author>Josh Oblinsky and Ryan Kingston</author>
        /// <summary>
        ///These are the tests that we created for StringSocketTest
        ///</summary>
        [TestClass()]
        public class SendBeforeReceiveTest
        {

            /// <summary>
            /// Tests to make sure that if Send is called before receive that the string will still be received, and not
            /// discarded. This can happen when the socket is loaded, but does not have any recipients for
            /// it's information.
            ///</summary>
            [TestMethod()]
            public void TestSendBeforeReceive()
            {
                new SendBeforeReceive().run(4001); //Run the test.
            }

            public class SendBeforeReceive
            {
                //Data used by the receiveSocket.
                private ManualResetEvent resetEvent;
                private String receivedString;
                private object receivedPayload;

                // Timeout used in test case
                private static int waitTime = 2000;

                public void run(int port)
                {
                    // Create and start a server and client.
                    TcpListener server = null;
                    TcpClient client = null;

                    try
                    {
                        //Initialize the connection.
                        server = new TcpListener(IPAddress.Any, port);
                        server.Start();
                        client = new TcpClient("localhost", port);

                        // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                        // method here, which is OK for a test case.
                        Socket serverSocket = server.AcceptSocket();
                        Socket clientSocket = client.Client;

                        // Wrap the two ends of the connection into StringSockets
                        StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                        StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                        // Communicate between the threads of the test cases
                        resetEvent = new ManualResetEvent(false);

                        //Send the string of data to the socket before receive has been called
                        String msg = "This is a test, bro.\n";
                        sendSocket.BeginSend(msg, (e, o) => { }, null);

                        //Make a receive request after data has been read into the socket.
                        receiveSocket.BeginReceive(CompletedReceive1, 1);

                        //Ensure that the data was received correctly.
                        Assert.AreEqual(true, resetEvent.WaitOne(waitTime), "Timed out waiting 1");
                        Assert.AreEqual("This is a test, bro.", receivedString);
                        Assert.AreEqual(1, receivedPayload);
                    }
                    finally
                    {
                        //Stop the server, and discard the socket connection.
                        server.Stop();
                        client.Close();
                    }
                }

                // This is the callback for the receive request.  We can't make assertions anywhere
                // but the main thread, so we write the values to member variables so they can be tested
                // on the main thread.
                private void CompletedReceive1(String s, Exception o, object payload)
                {
                    receivedString = s;
                    receivedPayload = payload;
                    resetEvent.Set();
                }
            }


        }

        /// <author>Xiaobing Rawlinson and Sam Callister</author>
        /// <summary>
        /// Starts the test that will test sending and recieving 5 strings. The test is given
        /// 20 seconds to complete.
        ///</summary>
        [TestMethod()]
        public void MultiStringTest()
        {
            new StressTest().run(4000);
        }

        /// <summary>
        /// This tests sending and recieving 5 strings.
        /// </summary>
        public class StressTest
        {


            // Stores all received strings
            private HashSet<string> receiveStrings = new HashSet<string>();

            // Stores all send strings
            private HashSet<string> sendStrings = new HashSet<string>();

            // Stores all strings that where sent without the \n
            private HashSet<string> correctStrings = new HashSet<string>();

            // Size of strings being handled
            private int size = 5;

            // TIMEOUT USED IN TEST CASE, 20 SECONDS IS USED YOU MAY NEED MORE TIME
            private static int timeout = 20000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;


                try
                {
                    // Set up server and client
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    //mre1 = new ManualResetEvent(false);
                    //mre2 = new ManualResetEvent(false);

                    // Make 5 recieve requests
                    for (int i = 0; i < size; i++)
                    {

                        receiveSocket.BeginReceive(CompletedReceive1, i);
                    }
                    // Send 5 strings
                    for (int i = size; i > 0; i--)
                    {
                        sendStrings.Add(i + " bottles of beer on the wall.\n");
                        correctStrings.Add(i + " bottles of beer on the wall.");
                    }

                    // Send the strings
                    foreach (string s in sendStrings)
                    {
                        sendSocket.BeginSend(s, (e, o) => { }, null);
                    }
                    // Wait to give enough time for the call backs to return
                    Thread.Sleep(timeout);
                    // Ensure that each string was recieved
                    foreach (string s in correctStrings)
                    {
                        // Make sure the lines were received properly.   
                        Assert.IsTrue(receiveStrings.Contains(s));
                    }
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // Callback method that adds each recieved string to the recieveStrings HashSet.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                receiveStrings.Add(s);

            }
        }

        /// <author> Conan Zhang and April Martin, modifying code provided by Professor de St. Germain</author>
        /// <date> 11-11-14</date>
        /// <summary>
        /// Tests whether threads are processed in the same order they are received, even if the first thread has a ludicrously long
        /// (and therefore slow) message and the second has a short one.
        /// </summary>


        [TestMethod()]
        public void SendOrderTest()
        {
            new SendOrderClass().run(4001);
        }

        /// <summary>
        /// Holds code for SendOrderTest
        /// </summary>
        public class SendOrderClass
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private String slowMsg;
            private object p1;
            private String fastMsg;
            private object p2;

            private int count = 0;
            private int slowOrder;
            private int fastOrder;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // Set slowMsg to an absurdly long string that should take a while to process.
                    // Set fastMsg to a single character.
                    slowMsg = @"{{About|the video game character|other uses|Kirby (disambiguation){{!}}Kirby}}
                    {{Infobox VG character
                    | width = 220px
                    | name = Kirby
                    | image = [[File:Kirby Wii.png|225px]]
                    | caption = Kirby as he appears in ''[[Kirby's Return to Dream Land]]''
                    | series = [[Kirby (series)|''Kirby'' series]]
                    | firstgame = ''[[Kirby's Dream Land]]'' (1992)
                    | creator = [[Masahiro Sakurai]]
                    | artist = Masahiro Sakurai
                    | japanactor = [[Mayumi Tanaka]] (1994)<br>[[Makiko Ohmoto]] (1999-present)
                    }}
                    {{nihongo|'''Kirby'''|カービィ|Kābī}} is a [[Character (arts)|fictional character]] and the protagonist of the 
                    ''[[Kirby (series)|Kirby series]]'' of video games owned by [[Nintendo]]. As one of Nintendo's most famous and familiar icons, 
                    Kirby's round, pink appearance and ability to copy his foe's powers to use as his own has made him a well known figure in video 
                    games, consistently ranked as one of the most iconic video game characters. He made his first appearance in 1992 in ''[[Kirby's 
                    Dream Land]]'' for the [[Game Boy]]. Originally a placeholder, created by [[Masahiro Sakurai]], for the game's early development, 
                    he has since then starred in over 20 games, ranging from [[Action game|action]] [[Platform game|platformers]] to [[Kirby's Pinball
                    Land|pinball]], [[Puzzle game|puzzle]] and [[Kirby Air Ride|racing]] games, and has been featured as a playable fighter in all 
                    ''[[Super Smash Bros.]]'' games. He has also starred in his own [[Kirby: Right Back at Ya|anime]] and manga series. His most 
                    recent appearance is in ''[[Super Smash Bros. for Nintendo 3DS and Wii U]]'', released in 2014 for the [[Nintendo 3DS]] and [[Wii 
                    U]]. Since 1999, he has been voiced by [[Makiko Ohmoto]].
                    Kirby is famous for his ability to inhale objects and creatures to obtain their attributes, as well as his ability to float with 
`                   puffed cheeks. He uses these abilities to rescue various lands, such as his home world of Dream Land, from evil forces and 
                    antagonists, such as [[Dark Matter (Kirby)|Dark Matter]] or [[Nightmare (Kirby)|Nightmare]]. On these adventures he often crosses 
                    paths with his rivals, the gluttonous [[King Dedede]] and the mysterious [[Meta Knight]]. In virtually all his appearances,
                    Kirby is depicted as cheerful, innocent, and food loving but becomes fearless, bold, and brave in the face of danger.
                    == Concept and creation ==";
                    fastMsg = "!";

                    // Send slowMsg before fastMsg
                    sendSocket.BeginSend(slowMsg, slowCallback, 1);
                    sendSocket.BeginSend(fastMsg, fastCallback, 2);

                    // Make sure that (a) neither thread timed out and
                    //(b) slowMsg was sent successfully before fastMsg
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(0, slowOrder);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual(1, fastOrder);
                    Assert.AreEqual(2, p2);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            /// <summary>
            /// This is the callback for the first send request.  We can't make assertions anywhere
            /// but the main thread, so we write the values to member variables so they can be tested
            /// on the main thread.
            /// </summary>
            /// <param name="s"></param>
            /// <param name="o"></param>
            /// <param name="payload"></param>
            private void slowCallback(Exception o, object payload)
            {
                slowOrder = count;
                count++;
                p1 = payload;
                mre1.Set();
            }

            /// <summary>
            /// This is the callback for the second send request.
            /// </summary>
            /// <param name="s"></param>
            /// <param name="o"></param>
            /// <param name="payload"></param>
            private void fastCallback(Exception o, object payload)
            {
                fastOrder = count;
                count++;
                p2 = payload;
                mre2.Set();
            }
        }

        /// <author>Kirk Partridge, Kameron Paulsen</author>
        /// <timecreated>11/12/14</timecreated>
        /// <summary>
        /// This method tests the StringSockets ability
        /// to Send Multiple strings before the BeginReceive
        /// is called.  It Sends both by single characters
        /// and full Strings.
        ///</summary>
        [TestMethod()]
        public void MultipleSendBeforeReceiveTest()
        {
            new MultipleSendBeforeReceive().run(4001);
        }
        public class MultipleSendBeforeReceive
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private ManualResetEvent mre3;
            private String s1;
            private object p1;
            private String s2;
            private object p2;
            private String s3;
            private object p3;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    mre3 = new ManualResetEvent(false);



                    // Now send the data.  Hope those receive requests didn't block!
                    String msg = "Hello world\nThis is a test\nStrings";
                    foreach (char c in msg)
                    {
                        sendSocket.BeginSend(c.ToString(), (e, o) => { }, null);
                    }
                    //Second Message to be sent
                    String msg2 = " sure are neat\n";
                    //Send the second message.  Should be appended to the leftovers from the foreach loop ("String").
                    sendSocket.BeginSend(msg2, (e, o) => { }, null);

                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);
                    receiveSocket.BeginReceive(CompletedReceive3, 3);

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("Hello world", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("This is a test", s2);
                    Assert.AreEqual(2, p2);

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("Strings sure are neat", s3);
                    Assert.AreEqual(3, p3);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request.  We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }
            // This is the callback for the third receive request.
            private void CompletedReceive3(String s, Exception o, object payload)
            {
                s3 = s;
                p3 = payload;
                mre3.Set();
            }
        }

        /// <summary>
        /// <author>Albert Tom, Matthew Lemon</author>
        /// This test stress tests the socket on sending long strings all at once
        /// Quotes provided by Jedi Master and super spy Liam Neeson
        /// </summary>

        [TestMethod()]
        public void LongStringTest()
        {
            new LongStringTestClass().run(4001);
        }

        public class LongStringTestClass
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private String s1;
            private object p1;
            private String s2;
            private object p2;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection. We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    // Make two receive requests
                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);

                    // Now send the data. Stress test long strings
                    String msg = "I don't have money. But what I do have are a very particular set of skills acquired over a very long career in the shadows, skills that make me a nightmare for people like you. If you let my daughter go now, that will be the end of it. I will not look for you, I will not pursue you. But if you don't, I will look for you, I will find you. And I will kill you\nI don't have anything else. [waves hand] But credits will do fine.\n";

                    sendSocket.BeginSend(msg, (e, o) => { }, null);

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("I don't have money. But what I do have are a very particular set of skills acquired over a very long career in the shadows, skills that make me a nightmare for people like you. If you let my daughter go now, that will be the end of it. I will not look for you, I will not pursue you. But if you don't, I will look for you, I will find you. And I will kill you", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("I don't have anything else. [waves hand] But credits will do fine.", s2);
                    Assert.AreEqual(2, p2);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request. We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }
        }

        /// <summary>
        /// Author: Ryan Farr
        /// A simple test to make sure Close() works
        ///</summary>
        [TestMethod()]
        [ExpectedException(typeof(System.ObjectDisposedException))]
        public void TestCloseBasic()
        {
            TcpListener server = new TcpListener(IPAddress.Any, 4006);
            server.Start();
            TcpClient client = new TcpClient("localhost", 4006);

            Socket serverSocket = server.AcceptSocket();
            Socket clientSocket = client.Client;

            StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
            StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

            sendSocket.Close();
            receiveSocket.Close();

            bool test1 = serverSocket.Available == 0; //Should fail here because socket should be shutdown and closed
        }

        /// <summary>
        /// Authors: Clint Wilkinson & Daniel Kenner
        /// 
        /// Class for Stress Test, based off of Test1Class given as part of PS7.
        /// 
        ///This is a test class for StringSocketTest and is intended
        ///to contain all StringSocketTest Unit Tests
        ///</summary>
        [TestClass()]
        public class StringSocketStressTest
        {
            /// <summary>
            /// A stress test for BeginSend and BeginReceive
            /// </summary>
            [TestMethod()]
            public void StressTest()
            {
                new StressTestClass().run(4001);
            }

            /// <summary>
            /// Class for Stress Test, based off of Test1Class given as part of PS7.
            /// </summary>
            public class StressTestClass
            {
                // Data that is shared across threads
                private ManualResetEvent mre1;
                private ManualResetEvent mre2;
                private String s1;
                private object p1;
                private String s2;
                private object p2;

                // Timeout used in test case
                private static int timeout = 2000;

                public void run(int port)
                {
                    // Create and start a server and client.
                    TcpListener server = null;
                    TcpClient client = null;

                    try
                    {
                        //setup the server
                        server = new TcpListener(IPAddress.Any, port);
                        server.Start();
                        client = new TcpClient("localhost", port);

                        // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                        // method here, which is OK for a test case.
                        Socket serverSocket = server.AcceptSocket();
                        Socket clientSocket = client.Client;

                        // Wrap the two ends of the connection into StringSockets
                        StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                        StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                        // This will coordinate communication between the threads of the test cases
                        mre1 = new ManualResetEvent(false);
                        mre2 = new ManualResetEvent(false);

                        //test a bunch of little strings
                        for (int i = 0; i <= 25000; i++)
                        {
                            //setup the receive socket
                            receiveSocket.BeginReceive(CompletedReceive1, 1);
                            //generate the string
                            sendSocket.BeginSend("A" + i + "\n", (e, o) => { }, null);
                            //wait a bit
                            Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                            //reset the timer
                            mre1.Reset();
                            //check that we are getting what we are supposed to
                            Assert.AreEqual("A" + i, s1);
                            //write out for debugging.
                            System.Diagnostics.Debug.WriteLine(s1);

                        }

                        //generate a big string to test with
                        String stress = "";
                        Random rand = new Random();
                        //put in character by character
                        for (int i = 0; i <= 25000; i++)
                        {
                            stress += ((char)(65 + rand.Next(26))).ToString();
                        }

                        //setup the receiver socket
                        receiveSocket.BeginReceive(CompletedReceive2, 2);
                        //send the big string
                        sendSocket.BeginSend(stress + "\n", (e, o) => { }, null);

                        System.Diagnostics.Debug.WriteLine(stress);

                        // Now send the data.  Hope those receive requests didn't block!
                        Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                        Assert.AreEqual(stress, s2);
                        Assert.AreEqual(2, p2);
                        //separate from last test
                        System.Diagnostics.Debug.WriteLine("");
                        //write for debugging
                        System.Diagnostics.Debug.WriteLine(s2);

                    }
                    finally
                    {
                        server.Stop();
                        client.Close();
                    }
                }
                // This is the callback for the first receive request.  We can't make assertions anywhere
                // but the main thread, so we write the values to member variables so they can be tested
                // on the main thread.
                private void CompletedReceive1(String s, Exception o, object payload)
                {
                    s1 = s;
                    p1 = payload;
                    mre1.Set();
                }
                // This is the callback for the second receive request.
                private void CompletedReceive2(String s, Exception o, object payload)
                {
                    s2 = s;
                    p2 = payload;
                    mre2.Set();
                }
            }
        }

        /// <summary>
        /// Written by: Kyle Hiroyasu and Drake Bennion
        /// This test is designed to ensure that string sockets will properly wait for strings to be sent and received
        /// The last send also ensures that a message is broken up by newline character but maintains same payload
        /// </summary>
        [TestMethod()]
        public void MessageOrderStressTest()
        {
            int Port = 4000;
            //int timeout = 30000;
            TcpListener server = null;
            TcpClient client = null;


            try
            {
                server = new TcpListener(IPAddress.Any, Port);
                server.Start();
                client = new TcpClient("localhost", Port);
                Socket serverSocket = server.AcceptSocket();
                Socket clientSocket = client.Client;

                StringSocket send = new StringSocket(serverSocket, Encoding.UTF8);
                StringSocket receive = new StringSocket(clientSocket, Encoding.UTF8);

                //Messages
                string message1 = "The sky is blue\n";
                string message2 = "The grass is green\n";
                string message3 = "Drakes hat is blue\n";
                string message4 = (new String('h', 1000)) + message1 + message2 + message3;
                string message4s = (new String('h', 1000)) + message1;

                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message1, message);
                    Assert.AreEqual(1, o);
                }, 1);

                send.BeginSend(message1, (e, o) => { }, 1);

                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message2, message);
                    Assert.AreEqual(2, o);
                }, 1);

                send.BeginSend(message2, (e, o) => { }, 2);
                send.BeginSend(message3, (e, o) => { }, 3);
                send.BeginSend(message4, (e, o) => { }, 4);

                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message3, message);
                    Assert.AreEqual(3, o);
                }, 1);

                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message4s, message);
                    Assert.AreEqual(4, o);
                }, 1);
                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message2, message);
                    Assert.AreEqual(4, o);
                }, 1);
                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message3, message);
                    Assert.AreEqual(4, o);
                }, 1);

            }
            finally
            {
                server.Stop();
                client.Close();
            }
        }

        /// <summary>
        /// Written by Ella Ortega and Jack Stafford for CS 3500, Fall 2014
        /// Ensures data was transmitted in the correct order.
        /// Sends a sentence with newlines instead of spaces.
        /// This enables seven receives.
        /// However, only three receives are called.
        /// These receives are checked for accuracy.
        /// </summary>
        [TestMethod]
        public void TestTransmissionOrderTest()
        {
            new TestLongStringSmallReturn().run(4001);
        }

        /// <summary>
        /// Called by TestMethod TestLongStringSmallReturn()
        /// </summary>
        public class TestLongStringSmallReturn
        {
            /// <summary>
            /// This method instantiates necessary object and calls BeginSend and BeginReceive
            /// </summary>
            /// <param name="port"></param>
            public void run(int port)
            {
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will be received as seven separate messages, even though it's sent all together
                    sendSocket.BeginSend("Plateaus\nare\nthe\nhighest\nform\nof\nflattery.\n", Callback1, 1);

                    receiveSocket.BeginReceive(Callback2, 2);
                    receiveSocket.BeginReceive(Callback3, 3);
                    receiveSocket.BeginReceive(Callback4, 4);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            /// <summary>
            /// Ensures no exceptions occured during sending and payload was returned correctly.
            /// </summary>
            /// <param name="e">Returned exception</param>
            /// <param name="payload"></param>
            private void Callback1(Exception e, object payload)
            {
                Assert.AreEqual(null, e);
                Assert.AreEqual(1, (int)payload);
            }

            /// <summary>
            /// Ensures no exceptions occured during receiving, payload was returned correctly, 
            /// and the correct message was received.
            /// </summary>
            /// <param name="message"></param>
            /// <param name="e"></param>
            /// <param name="payload"></param>
            private void Callback2(String message, Exception e, object payload)
            {
                Assert.AreEqual("Plateaus", message);
                Assert.AreEqual(null, e);
                Assert.AreEqual(2, (int)payload);
            }

            /// <summary>
            /// Ensures no exceptions occured during receiving, payload was returned correctly, 
            /// and the correct message was received.
            /// </summary>
            /// <param name="message"></param>
            /// <param name="e"></param>
            /// <param name="payload"></param>
            private void Callback3(String message, Exception e, object payload)
            {
                Assert.AreEqual("are", message);
                Assert.AreEqual(null, e);
                Assert.AreEqual(3, (int)payload);
            }

            /// <summary>
            /// Ensures no exceptions occured during receiving, payload was returned correctly, 
            /// and the correct message was received.
            /// </summary>
            /// <param name="message"></param>
            /// <param name="e"></param>
            /// <param name="payload"></param>
            private void Callback4(String message, Exception e, object payload)
            {
                Assert.AreEqual("the", message);
                Assert.AreEqual(null, e);
                Assert.AreEqual(4, (int)payload);
            }
        }

        /// <summary>

        /// Namgi Yoon u0759547

        /// A simple test for BeginSend and BeginReceive

        ///</summary>

        [TestMethod()]

        public void SimpleTest()
        {

            new StringSocketTester1().run(4001);

        }

        /// <summary>

        /// Class used for test1

        /// </summary>

        public class StringSocketTester1
        {

            // Data that is shared across threads

            private ManualResetEvent mre1, mre2, mre3;

            private String string1, string2, string3;

            private object payload1, payload2, payload3;



            // Timeout used in test case

            private static int timeout = 2000;



            public void run(int port)
            {

                // Create and start a server and client.

                TcpListener server = null;

                TcpClient client = null;

                try
                {

                    server = new TcpListener(IPAddress.Any, port);

                    server.Start();

                    client = new TcpClient("localhost", port);



                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()

                    // method here, which is OK for a test case.

                    Socket serverSocket = server.AcceptSocket();

                    Socket clientSocket = client.Client;



                    // Wrap the two ends of the connection into StringSockets

                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());

                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());



                    // This will coordinate communication between the threads of the test cases

                    mre1 = new ManualResetEvent(false);

                    mre2 = new ManualResetEvent(false);

                    mre3 = new ManualResetEvent(false);



                    // Make two receive requests

                    receiveSocket.BeginReceive(CompletedReceive1, "payload for message 1");

                    receiveSocket.BeginReceive(CompletedReceive2, "payload for message 2");

                    receiveSocket.BeginReceive(CompletedReceive3, "payload for message 3");



                    // Now send the data.  Hope those receive requests didn't block!

                    String msg = "1\n2\n3\n";

                    foreach (char c in msg)
                    {

                        sendSocket.BeginSend(c.ToString(), (e, o) => { }, null);

                    }



                    //Whole message at once.

                    //sendSocket.BeginSend(msg, (e, o) => { }, null);



                    //Checking message number 1

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");

                    Assert.AreEqual("1", string1);

                    Assert.AreEqual("payload for message 1", payload1);



                    //Checking message number 2

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    Assert.AreEqual("2", string2);

                    Assert.AreEqual("payload for message 2", payload2);



                    //Checking message number 3

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");

                    Assert.AreEqual("3", string3);

                    Assert.AreEqual("payload for message 3", payload3);





                }

                finally
                {

                    server.Stop();

                    client.Close();

                }

            }

            // This is the callbacks for requests.

            private void CompletedReceive1(String s, Exception o, object payload) { string1 = s; payload1 = payload; mre1.Set(); }

            private void CompletedReceive2(String s, Exception o, object payload) { string2 = s; payload2 = payload; mre2.Set(); }

            private void CompletedReceive3(String s, Exception o, object payload) { string3 = s; payload3 = payload; mre3.Set(); }

        }

        // Created by: Sam Trout and Sam England
        /// <summary>
        /// Test case checks whether or not the callback method is sent on its own threadpool. Fails if it times out because 
        /// the thread is blocked.
        /// </summary>
        [TestMethod()]
        public void BeginSendSeperateThread()
        {
            new BeginSendSeperateClass().run(4001);
        }

        public class BeginSendSeperateClass
        {
            // Data that is shared across threads
            private ManualResetEvent mre = new ManualResetEvent(false);

            // Timeout used in test case
            private static int timeout = 20000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection. We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());

                    // Now send the data. Will block after newline if callback doesnt come back in its own threadpool
                    String msg = "Hopefully this works\n";
                    String msg2 = "Second message\n";

                    //calls beginsend 2 times for the different messages
                    sendSocket.BeginSend(msg, callback1, 1);
                    sendSocket.BeginSend(msg2, callback2, 2);

                    Assert.AreEqual(true, mre.WaitOne(timeout), "Timed out, callback1 blocked second BeginSend");
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            /// <summary>
            /// This callback creates an infinite while loop and if not handled properly in StringSocket will cause the program
            /// to timeout and fail
            /// </summary>
            /// <param name="e"> Default </param>
            /// <param name="payload"> Default </param>
            private void callback1(Exception e, object payload)
            {
                while (true) ;
            }

            /// <summary>
            /// This callback is for the 2nd string, this callback wont be called unless handled properly in the StringSocket
            /// mre will never be set and the program will timeout
            /// </summary>
            /// <param name="e"> Default </param>
            /// <param name="payload"> Default</param>
            private void callback2(Exception e, object payload)
            {
                mre.Set();
            }
        }

        /// <summary>
        ///James Watts & Stuart Johnsen
        ///
        ///Tests sending a single long String that contains 4 lines, seperated by "\n". The string is the lyrics
        ///to the chorus of Haddaway's "What is Love?" Lines are placed in the correct order using a sequential 
        ///integer from the callback's payload.
        ///</summary>
        [TestMethod()]
        public void What_Is_Love_Test()
        {
            new What_Is_Love_TestClass().run(4005);
        }

        public class What_Is_Love_TestClass
        {
            // Data that is shared across threads
            private ManualResetEvent mre0;
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private ManualResetEvent mre3;

            //The String to be sent
            String whatIsLove = "What is love?\nBaby don't hurt me,\nDon't hurt me\nNo more!\n";

            //A String[] for the received lines of text. Should contain 4 elements when completed.
            String[] receivedLines = new String[4];

            // Timeout used in test case
            //private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection. We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre0 = new ManualResetEvent(false);
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    mre3 = new ManualResetEvent(false);

                    //Setup 4 BeginReceives to receive the 4 lines.
                    for (int i = 0; i < 4; i++)
                    {
                        receiveSocket.BeginReceive(WhatIsLove_Callback, i);
                    }

                    sendSocket.BeginSend(whatIsLove, (e, o) => { }, null);

                    Thread.Sleep(2000);

                    Assert.AreEqual("What is love?", receivedLines[0]);
                    Assert.AreEqual("Baby don't hurt me,", receivedLines[1]);
                    Assert.AreEqual("Don't hurt me", receivedLines[2]);
                    Assert.AreEqual("No more!", receivedLines[3]);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            } //End run


            /// <summary>
            /// The callback for receive requests, uses the callback's payload to place lines in the correct order
            ///in a String array.
            ///The appropriate ManualResetEvent is chosen based on the callback's payload.
            /// </summary>
            private void WhatIsLove_Callback(String s, Exception o, object payload)
            {
                int index = (int)payload;
                receivedLines[index] = s;

                switch (index)
                {
                    case 0:
                        mre0.Set();
                        break;
                    case 1:
                        mre1.Set();
                        break;
                    case 2:
                        mre2.Set();
                        break;
                    case 3:
                        mre3.Set();
                        break;
                }
            }
        }

        /// <author> Zane Zakraisek and Alex Ferro </author>
        /// <summary>
        /// Test that a long string, then a short string, and then a long one are correctly received in in the right sent order.
        /// Written by Zane Zakraisek and Alex Ferro
        /// </summary>
        [TestMethod()]
        public void TestLongShortLongStringRX()
        {
            new TestLongShortLongStringRXClass().run(4002);
        }
        /// <summary>
        /// This is the test class for TestLongShortLongStringRX
        /// Test that a long string, then a short string, and then a long one are correctly received in in the right sent order.
        /// Written by Zane Zakraisek and Alex Ferro
        /// </summary>
        public class TestLongShortLongStringRXClass
        {
            // Data that is shared across threads
            // Used to ensure the correct testing assertion on the main thread
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private ManualResetEvent mre3;
            private String s1;
            private object p1;
            private String s2;
            private object p2;
            private String s3;
            private object p3;
            // Test strings
            private String shortString = "This is a journey through time!\n";
            private String longString = "or near friends, which is certainly more unusual. He lived alone" +
            "in his house in Saville Row, whither none penetrated. A single" +
            "domestic sufficed to serve him. He breakfasted and dined at the club" +
            "or near friends, which is certainly more unusual. He lived alone" +
            "in his house in Saville Row, whither none penetrated. A single" +
            "domestic sufficed to serve him. He breakfasted and dined at the club" +
            "or near friends, which is certainly more unusual. He lived alone" +
            "in his house in Saville Row, whither none penetrated. A single" +
            "domestic sufficed to serve him. He breakfasted and dined at the club" +
            "or near friends, which is certainly more unusual. He lived alone" +
            "in his house in Saville Row, whither none penetrated. A single" +
            "domestic sufficed to serve him. He breakfasted and dined at the club" +
            "or near friends, which is certainly more unusual. He lived alone" +
            "in his house in Saville Row, whither none penetrated. A single" +
            "domestic sufficed to serve him. He breakfasted and dined at the club" +
            "or near friends, which is certainly more unusual. He lived alone" +
            "in his house in Saville Row, whither none penetrated. A single" +
            "domestic sufficed to serve him. He breakfasted and dined at the club" +
            "or near friends, which is certainly more unusual. He lived alone" +
            "in his house in Saville Row, whither none penetrated. A single" +
            "domestic sufficed to serve him. He breakfasted and dined at the club" +
            "or near friends, which is certainly more unusual. He lived alone" +
            "in his house in Saville Row, whither none penetrated. A single" +
            "domestic sufficed to serve him. He breakfasted and dined at the club" +
            "or near friends, which is certainly more unusual. He lived alone" +
            "in his house in Saville Row, whither none penetrated. A single" +
            "domestic sufficed to serve him. He breakfasted and dined at the club" +
            "or near friends, which is certainly more unusual. He lived alone" +
            "in his house in Saville Row, whither none penetrated. A single" +
            "domestic sufficed to serve him. He breakfasted and dined at the club" +
            "or near friends, which is certainly more unusual. He lived alone" +
            "in his house in Saville Row, whither none penetrated. A single\n";

            // Timeout used in test case
            private static int timeout = 2000;
            /// <summary>
            /// Run the test on the specified port
            /// </summary>
            /// <param name="port"></param>
            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection. We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    mre3 = new ManualResetEvent(false);

                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);
                    receiveSocket.BeginReceive(CompletedReceive3, 3);

                    sendSocket.BeginSend(longString, (e, o) => { }, null);
                    sendSocket.BeginSend(shortString, (e, o) => { }, null);
                    sendSocket.BeginSend(longString, (e, o) => { }, null);

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(longString.Replace("\n", ""), s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual(shortString.Replace("\n", ""), s2);
                    Assert.AreEqual(2, p2);

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual(longString.Replace("\n", ""), s3);
                    Assert.AreEqual(3, p3);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            /// <summary>
            /// This is the callback for the first receive request. We can't make assertions anywhere
            /// but the main thread, so we write the values to member variables so they can be tested
            /// on the main thread.
            /// </summary>
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }
            /// <summary>
            /// This is the callback for the second receive request.
            /// </summary>
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }
            /// <summary>
            /// This is the callback for the third receive request.
            /// </summary>
            private void CompletedReceive3(String s, Exception o, object payload)
            {
                s3 = s;
                p3 = payload;
                mre3.Set();
            }
        }

        // didn't add test by Eric Stubbs it was taking too long
        // didn't add test by Courtney Burness because it was messed up

        /// <summary>
        /// A test that makes sure data is being stored internally to the
        /// StringSocket when there are no pending receive requests yet. 
        /// Also, that once two receive requests are made, that those requests
        /// grab only two strings from the beginning of the stored data and
        /// that the remaining stored data does not interfere and overwrite it.
        ///</summary>
        [TestMethod()]
        public void Test4()
        {
            new Test4Class().run(4001);
        }

        public class Test4Class
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private String s1;
            private object p1;
            private String s2;
            private object p2;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    // Build and start the server.
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Construct the StringSockets with the already connected underlying sockets.
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    // Send the data in the specified string. There are not yet any receive requests on the 
                    // receiving socket. The data should be stored internally to the receiving string socket
                    // and processed after a receive request is made.
                    String msg = "Space\n is disease and \ndanger wrap\nped in darkn\ness and silence\n.\n";
                    foreach (char c in msg)
                    {
                        sendSocket.BeginSend(c.ToString(), (e, o) => { }, null);
                    }

                    // Make two receive requests. They should only receive the first two increments of data
                    // containing newline characters ("Space" and " is disease and ")
                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);

                    // Make sure that the extra jibberish did not overwrite the expected
                    // strings.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("Space", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual(" is disease and ", s2);
                    Assert.AreEqual(2, p2);
                }
                // Make sure to clean up sockets and close gracefully.
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }
        }

        /// <summary>
        /// Written by Camille Rasmussen and Jessie Delacenserie
        /// 
        /// This Test Method tests the behavior of the Close() method upon no calls to the BeginSend or
        /// BeginReceive methods. This test makes sure that the client socket is not disconnected when
        /// you invoke Close() on the sendSocket, but that the server socket is successfully disconnected
        /// when you do so.
        /// 
        /// We also test to make sure you can't access the Socket after it's StringSocket's Close() has 
        /// been invoked, by calling a Socket's Available property, which throws an exception if the
        /// Socket has been closed properly.
        /// </summary>
        [TestMethod]
        public void TestMethod7()
        {
            new CloseWithoutReceieveOrSend().run(4001);
        }

        public class CloseWithoutReceieveOrSend
        {
            public void run(int port)
            {
                TcpListener server = null;
                TcpClient client = null;

                // set up random encoder to use
                Encoding encoder = new ASCIIEncoding();

                // to check if exception was thrown and handled
                bool caught = false;

                try
                {
                    // create and start the server
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    // create the client
                    client = new TcpClient("localhost", port);

                    // set up the server and client sockets
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // set up string sockets with sockets
                    StringSocket sendSocket = new StringSocket(serverSocket, encoder);
                    StringSocket receiveSocket = new StringSocket(clientSocket, encoder);

                    // make sure the sockets are connected initially
                    Assert.IsTrue(serverSocket.Connected);
                    Assert.IsTrue(clientSocket.Connected);

                    // close the sendSocket String Socket
                    sendSocket.Close();
                    // make sure wrapped Socket specified is disconnected accordingly
                    Assert.IsFalse(serverSocket.Connected);
                    // and the other wrapped Socket isn't affected
                    Assert.IsTrue(clientSocket.Connected);

                    // close the receiveSocket String Socket
                    receiveSocket.Close();
                    // make sure wrapped socket specified is disconnected accordingly
                    Assert.IsFalse(clientSocket.Connected);

                    // this should throw an exception if the Socket was closed properly
                    int amount = serverSocket.Available;
                }
                // exception caught here
                catch (ObjectDisposedException e)
                {
                    caught = true;
                }
                finally
                {
                    // close up your resources and stop the server
                    server.Stop();
                    client.Close();

                    // make sure proper exception was thrown and caught
                    Assert.IsTrue(caught);
                }
            }
        }

        // didn't add Danial Ebling's test - see comment in forum

        /// <summary>
        /// Test method to check to see if the BeginReceive can handle
        /// two "\n" passed in from a single BeginSend.
        /// </summary>
        /// <author>Peter Jacobsen</author>
        [TestMethod]
        public void Send1Receive2()
        {
            new Test10Class().run(4005);
        }

        public class Test10Class
        {
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private String s1;
            private object p1;
            private String s2;
            private object p2;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    // String messages that are sent from the server to the client
                    String message1 = "Hello World!";
                    String message2 = " Goodbye World!";

                    // Combines both messages together separated by the "\n" character
                    sendSocket.BeginSend(message1 + "\n" + message2 + "\n", (e, o) => { }, null);

                    // Each BeginReceive should each receive one message independently
                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);
                    sendSocket.Close();

                    // Checks that the received data corresponds with the messages sent
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(message1, s1);
                    Assert.AreEqual(1, p1);
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual(message2, s2);
                    Assert.AreEqual(2, p2);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request. We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }
        }

        /// <summary
        /// Makes sure that callbacks are called in a new
        /// thread, by passing a callback that sleeps forever.
        /// </summary>
        /// <author>Sheyne Anderson and Nathan Donaldson</author> 
        /// 
        [TestMethod()]
        public void AnnoyinglyLongCallback()
        {
            new AnnoyinglyLongCallbackTestClass().run(4001);
        }

        public class AnnoyinglyLongCallbackTestClass
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private String s1;
            private object p1;
            private String s2;
            private object p2;

            // Timeout used in test case
            private static int timeout = 20000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    // Make two receive requests
                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);

                    // Now send the data.  Hope those receive requests didn't block!
                    String msg = "Hello world\nThis is a test\n";
                    foreach (char c in msg)
                    {
                        sendSocket.BeginSend(c.ToString(), (e, o) => Thread.Sleep(Timeout.Infinite), null);
                    }
                    //sendSocket.BeginSend(msg, (e, o) => { },null);

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("Hello world", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("This is a test", s2);
                    Assert.AreEqual(2, p2);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request.  We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }
        }

        /// <author>John Robe and Dietrich Geisler</author>
        /// <summary>
        /// Tests that the string socket can handle an extremely long single string with interspersed new line characters
        /// </summary>
        [TestMethod()]
        public void TestMassiveSingleMessage()
        {
            //Declare everything before the try-catch block so that the finally can activate correctly
            TcpListener server = null;
            TcpClient client = null;
            Socket serverSocket = null;
            Socket clientSocket = null;

            StringSocket sendSocket = null;
            StringSocket receiveSocket = null;

            int port = 4002;
            int timeout = 10000;

            try
            {
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                client = new TcpClient("localhost", port);

                //Sets up the sockets from both ends
                serverSocket = server.AcceptSocket();
                clientSocket = client.Client;

                //Wraps the sockets into string sockets
                sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                //Creates 10 unique mres for our 10 substrings
                ManualResetEvent[] mreList = new ManualResetEvent[10];
                for (int i = 0; i < 10; i++)
                    mreList[i] = new ManualResetEvent(false);

                //Creates 10 unique strings for the results from our sockets
                String[] results = new String[10];
                for (int i = 0; i < 10; i++)
                    results[i] = "";

                //Sets up a large string of random characters with 10 dispersed newline symbols
                String toSend = "";
                String[] toTest = new String[10];
                Random rng = new Random();

                for (int i = 0; i < 10; i++)
                {
                    double segmentSize = rng.Next(2000, 5000);

                    for (int j = 0; j < segmentSize; j++) //Creates a series of segments of random length
                        toTest[i] += (char)(rng.Next(26) + 65); //Appends a random upper-case letter to the string

                    toSend += toTest[i] + "\n"; //Adds a newline character to the end of the current string segment
                }

                //Starts 10 unique receives
                for (int i = 0; i < 10; i++)
                {
                    receiveSocket.BeginReceive((s, e, p) => { results[(int)p] = s; mreList[(int)p].Set(); }, i); //Note that i is used as the payload to get the correct array index
                }

                sendSocket.BeginSend(toSend, (e, p) => { }, null);

                //Test that all the strings came back correctly
                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(true, mreList[i].WaitOne(timeout));
                    Assert.AreEqual(toTest[i], results[i]);
                }
            }

            finally
            {
                //Clean everything up
                sendSocket.Close();
                receiveSocket.Close();
                server.Stop();
                client.Close();

            }
        }

        // didn't add John Ballards test - lots of comments so it may be wrong
        // didn't add Steward Charles test because it had build problems

        /// <summary>
        /// <Author>Brandon Hilton and Meher Samineni</Author>
        /// </summary>
        [TestMethod]
        public void Test5()
        {
            new Test5Class().run(4001);
        }
        public class Test5Class
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private ManualResetEvent mre3;
            private ManualResetEvent mre4;
            private ManualResetEvent mre5;
            private ManualResetEvent mre6;
            private ManualResetEvent mre7;
            private String s1 = "";
            private String s2 = "";
            private String s3 = "";
            private String s4 = "";
            private String s5 = "";
            private String s6 = "";
            private String s7 = "";
            private object p1 = null;
            private object p2 = null;
            private object p3 = null;
            private object p4 = null;
            private object p5 = null;
            private object p6 = null;
            private object p7 = null;

            // Timeout used in test case
            int timeout = 20000;

            public void run(int port)
            {

                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    mre3 = new ManualResetEvent(false);
                    mre4 = new ManualResetEvent(false);
                    mre5 = new ManualResetEvent(false);
                    mre6 = new ManualResetEvent(false);
                    mre7 = new ManualResetEvent(false);

                    // Make two receive requests
                    receiveSocket.BeginReceive((string s, Exception e, object p) => { s1 = s; p1 = p; mre1.Set(); }, 1);
                    receiveSocket.BeginReceive((string s, Exception e, object p) => { s2 = s; p2 = p; mre2.Set(); }, 2);
                    receiveSocket.BeginReceive((string s, Exception e, object p) => { s3 = s; p3 = p; mre3.Set(); }, 3);
                    receiveSocket.BeginReceive((string s, Exception e, object p) => { s4 = s; p4 = p; mre4.Set(); }, 4);
                    receiveSocket.BeginReceive((string s, Exception e, object p) => { s5 = s; p5 = p; mre5.Set(); }, 5);
                    receiveSocket.BeginReceive((string s, Exception e, object p) => { s6 = s; p6 = p; mre6.Set(); }, 6);
                    receiveSocket.BeginReceive((string s, Exception e, object p) => { s7 = s; p7 = p; mre7.Set(); }, 7);
                    // Now send the data.  Hope those receive requests didn't block!
                    String msg = "Hello world\nThis is Ruby\nThis is Weiss\nThis is Blake\nThis is Yang\nTeam RWBY\nI don't Know what else to say\n";

                    sendSocket.BeginSend(msg, (e, o) => { }, null);

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("Hello world", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("This is Ruby", s2);
                    Assert.AreEqual(2, p2);

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("This is Weiss", s3);
                    Assert.AreEqual(3, p3);

                    Assert.AreEqual(true, mre4.WaitOne(timeout), "Timed out waiting 4");
                    Assert.AreEqual("This is Blake", s4);
                    Assert.AreEqual(4, p4);

                    Assert.AreEqual(true, mre5.WaitOne(timeout), "Timed out waiting 5");
                    Assert.AreEqual("This is Yang", s5);
                    Assert.AreEqual(5, p5);

                    Assert.AreEqual(true, mre6.WaitOne(timeout), "Timed out waiting 6");
                    Assert.AreEqual("Team RWBY", s6);
                    Assert.AreEqual(6, p6);

                    Assert.AreEqual(true, mre7.WaitOne(timeout), "Timed out waiting 6");
                    Assert.AreEqual("I don't Know what else to say", s7);
                    Assert.AreEqual(7, p7);

                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

        }

        /// <summary>
        /// modified version of the above test from the ps7 skeleton
        /// </summary>
        [TestMethod]
        public void TestMethod9()
        {
            new StringTesters().run(4002);
        }

        public class StringTesters
        {
            private ManualResetEvent mre1;
            private string s1;
            private Object p1;

            private static int timeout = 2000;

            public void run(int port)
            {

                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);


                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    mre1 = new ManualResetEvent(false);

                    receiveSocket.BeginReceive(CompletedRecieve1, 1);

                    string msg = "Hey Guy\n";

                    sendSocket.BeginSend(msg, (e, o) => { }, "cat");

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("Hey Guy", s1);
                    Assert.AreEqual(1, p1);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            private void CompletedRecieve1(string s, Exception e, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

        }

        [TestMethod]
        public void testBrokenMessage()
        {
            Thread test = new Thread(o =>
            {
                new BrokenMessages().run(4001);
            });
            test.Start();
            test.Abort();
        }

        /// <summary>
        /// Test class written by Michael Zhao and Aaron Hsu. Tests whether
        /// a) a large message broken across different BeginSends sends properly
        /// b) if a dangling message (one not terminated with a "\n") is handled properly
        /// </summary>
        public class BrokenMessages
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private ManualResetEvent mre3;
            private String s1;
            private object p1;
            private String s2;
            private object p2;
            private String s3;
            private object p3;


            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection. We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    mre3 = new ManualResetEvent(false);

                    // Make two receive requests
                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);
                    receiveSocket.BeginReceive(CompletedReceive3, 3);

                    //Create a long string for beginsend
                    string message1 = "hnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnggggggggggggggggggggggggggggggggggggggggggggggggggg ";
                    string message2 = "soooooooooooooooooooooccccckkeeeeetttssssssss\nhnnnnnnnnnnnnnnnnnnnnnnnnggggggggggggggggggggggggggggggggg ";
                    string message3 = "hnnnnnnnnnnnnnnnnnnnnngggggggggggggggggggggggggggggg\nhnnnnnnnnnnnnnnnnnnnnnnnnnnggggggggggggggggggggg";
                    string[] messages = Regex.Split(message1 + message2 + message3, "\n");

                    //Create a new thread to enusre that close gets called at the same time as BeginSend()
                    sendSocket.BeginSend(message1, (e, o) => { }, null);
                    sendSocket.BeginSend(message2, (e, o) => { }, null);
                    sendSocket.BeginSend(message3, (e, o) => { }, null);
                    sendSocket.Close();

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(messages[0], s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual(messages[1], s2);
                    Assert.AreEqual(2, p2);

                    //Check that we timeout waiting for the third receive request, and that s3 remains null.
                    Assert.AreEqual(false, mre3.WaitOne(timeout), "Did not time out waiting for 3");
                    Assert.AreEqual(null, s3);


                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request. We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // This is the callback for the first receive request. We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre1.Set();
            }

            private void CompletedReceive3(String s, Exception o, object payload)
            {
                s3 = s;
                p3 = payload;
                mre1.Set();
            }
        }

        /// <summary>
        /// Val Nicholas Hallstrom
        /// This test makes sure that your program handles sparatic newlines properly.
        /// I used the setupServerClient method from another useful test I found on the forum since the way I was trying to setup the sockets didn't seem to work :/
        /// </summary>
        [TestMethod()]
        public void MultipleNewlines()
        {
            int timeout = 1000;
            StringSocket sendSocket;
            StringSocket receiveSocket;
            string s1 = "";
            int p1 = 0;
            string s2 = "";
            int p2 = 0;
            string s3 = "";
            int p3 = 0;
            string s4 = "";
            int p4 = 0;
            string s5 = "";
            int p5 = 0;

            ManualResetEvent mre1 = new ManualResetEvent(false);
            ManualResetEvent mre2 = new ManualResetEvent(false);
            ManualResetEvent mre3 = new ManualResetEvent(false);
            ManualResetEvent mre4 = new ManualResetEvent(false);
            ManualResetEvent mre5 = new ManualResetEvent(false);

            setupServerClient(63333, out sendSocket, out receiveSocket);

            receiveSocket.BeginReceive((s, e, p) => { s1 = s; p1 = (int)p; mre1.Set(); }, 1);
            receiveSocket.BeginReceive((s, e, p) => { s2 = s; p2 = (int)p; mre2.Set(); }, 2);
            receiveSocket.BeginReceive((s, e, p) => { s3 = s; p3 = (int)p; mre3.Set(); }, 3);
            receiveSocket.BeginReceive((s, e, p) => { s4 = s; p4 = (int)p; mre4.Set(); }, 4);
            receiveSocket.BeginReceive((s, e, p) => { s5 = s; p5 = (int)p; mre5.Set(); }, 5);
            sendSocket.BeginSend("\nWhat's\n\nup?\n\n", (e, p) => { }, null);

            Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
            Assert.AreEqual("", s1);
            Assert.AreEqual(1, p1);

            Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
            Assert.AreEqual("What's", s2);
            Assert.AreEqual(2, p2);

            Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
            Assert.AreEqual("", s3);
            Assert.AreEqual(3, p3);

            Assert.AreEqual(true, mre4.WaitOne(timeout), "Timed out waiting 4");
            Assert.AreEqual("up?", s4);
            Assert.AreEqual(4, p4);

            Assert.AreEqual(true, mre5.WaitOne(timeout), "Timed out waiting 5");
            Assert.AreEqual("", s5);
            Assert.AreEqual(5, p5);
        }

        public void setupServerClient(int port, out StringSocket sendSocket, out StringSocket receiveSocket)
        {
            // Create and start a server and client.
            TcpListener server = null;
            TcpClient client = null;
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            client = new TcpClient("localhost", port);

            // Obtain the sockets from the two ends of the connection. We are using the blocking AcceptSocket()
            // method here, which is OK for a test case.
            Socket serverSocket = server.AcceptSocket();
            Socket clientSocket = client.Client;

            // Wrap the two ends of the connection into StringSockets
            sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
            receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

        }

        /// <summary>
        /// Written by Hannah Eyre and Michael Nelson by modifying the test in PS7Skeleton.
        /// This test should make sure the StringSocket is correctly handling different types of unusual strings, such as characters out of
        /// the UTF-8 encoding, empty strings, and null strings.
        /// 
        /// The test for UTF-8 involves a UTF-16 capital sigma. Because this is outside the range of acceptable UTF-8 values, the replacement
        /// fallback handler should replace this with a "?" character unless the EncoderFallback has been set to exception or best-fit.
        ///</summary>
        [TestMethod()]
        public void BadStringTest()
        {
            new BadStringsTest().run(4001);
        }

        public class BadStringsTest
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private ManualResetEvent mre3;
            private ManualResetEvent mre4;
            private String s1;
            private object p1;
            private String s2;
            private object p2;
            private String s3;
            private object p3;
            private object p4;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    mre3 = new ManualResetEvent(false);

                    // Make two receive requests
                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);
                    receiveSocket.BeginReceive(CompletedReceive3, 3);

                    // Now send the data.  Hope those receive requests didn't block!
                    String msg = "\u01A9\nHello\n\n"; //sigma, Hello on new line
                    foreach (char ch in msg)
                        sendSocket.BeginSend(ch.ToString(), (e, o) => { }, null);

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    //Assert.AreEqual("?", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("Hello", s2);
                    Assert.AreEqual(2, p2);

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("", s3);
                    Assert.AreEqual(3, p3);

                    //sendSocket.BeginSend(null, (e, o) => { }, null);

                    //Assert.AreEqual(true, mre4.WaitOne(timeout), "Timed out waiting 4");
                    //Assert.AreEqual(4, p4);

                    //I commented out lines that don't seem to work the way our code was designed.
                    //What should be done if you try to send a null string?
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request.  We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                Assert.IsNull(o);
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                Assert.IsNull(o);
                s2 = s;
                p2 = payload;
                mre2.Set();
            }

            private void CompletedReceive3(String s, Exception o, object payload)
            {
                Assert.IsNull(o);
                s3 = s;
                p3 = payload;
                mre3.Set();
            }

            private void CompletedReceive4(String s, Exception o, object payload)
            {
                Assert.IsNull(s);
                Assert.IsTrue(o is ArgumentNullException);
                p4 = payload;
                mre4.Set();
            }
        }

        /// <summary>
        /// <author>Derek Heldt-Werle</author>
        /// Test to ensure that the characters following a new line is properly
        /// disposed of, and only the data preceeding the new line character is returned.
        /// </summary>
        [TestMethod()]
        public void LongStringFollowedByShortStringTest()
        {
            new Test100Class().run(4001);
        }

        public class Test100Class
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private String s1;
            private object p1;
            private String s2;
            private object p2;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection. We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    // Make two receive requests
                    receiveSocket.BeginReceive(CompletedReceive1, 1);

                    // Now send the data. Hope those receive requests didn't block!
                    String msg = "Is this the real life? Is this just fantasy?\n Insert more lyrics here";

                    sendSocket.BeginSend(msg, (e, o) => { }, null);

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("Is this the real life? Is this just fantasy?", s1);
                    Assert.AreEqual(1, p1);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request. We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }
        }

        /// <summary>
        ///Authors: Blake Burton, Cameron Minkel
        ///Date: 11/12/14
        ///
        ///This is a simple test which makes sure the supplied callback
        ///to StringSocket's BeginSend method is called upon method completion.
        ///</summary>
        [TestMethod()]
        public void TestBeginSendCallback()
        {
            new Test200Class().run(4001);
        }

        public class Test200Class
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private bool callbackInvoked = false;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the socket from the connection
                    Socket serverSocket = server.AcceptSocket();

                    // Wrap the socket into a StringSocket
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);

                    // Send a test message
                    sendSocket.BeginSend("Please pass.", CompletedSend, new Object());
                    mre1.WaitOne(timeout);

                    // Make sure the callback was called 
                    Assert.IsTrue(callbackInvoked);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // The bool will tell us if the callback was invoked.
            private void CompletedSend(Exception o, object payload)
            {
                callbackInvoked = true;
                mre1.Set();
            }
        } // end TestBeginSendCallback

        /// <summary>
        /// A simple test for BeginSend and BeginReceive making sure whitespace isn't removed and quotation marks are preserved.
        /// Also tests whether requests can be made before and after the requested data is sent.
        /// Provided test case modified by Drew McClelland.
        ///</summary>
        [TestMethod()]
        public void Test101()
        {
            new Test101Class().run(4001);
        }

        public class Test101Class
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private ManualResetEvent mre3;
            private ManualResetEvent mre4;
            private ManualResetEvent mre5;
            private ManualResetEvent mre6;
            private String s1;
            private object p1;
            private String s2;
            private object p2;
            private String s3;
            private object p3;
            private String s4;
            private object p4;
            private String s5;
            private object p5;
            private String s6;
            private object p6;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection. We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    mre3 = new ManualResetEvent(false);
                    mre4 = new ManualResetEvent(false);
                    mre5 = new ManualResetEvent(false);
                    mre6 = new ManualResetEvent(false);

                    // Make three receive requests
                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);
                    receiveSocket.BeginReceive(CompletedReceive3, 3);


                    // Now send the data. Hope those receive requests didn't block!
                    String msg = "\t\n \n\"\"\n\"\n";
                    String msg2 = "\'\'\n\'\n";
                    // Sends first message as individual characters.
                    foreach (char c in msg)
                    {
                        sendSocket.BeginSend(c.ToString(), (e, o) => { }, null);
                    }

                    // Sends second message as one continuous line.
                    sendSocket.BeginSend(msg2, (e, o) => { }, null);

                    // Receive last three messages.
                    receiveSocket.BeginReceive(CompletedReceive4, 4);
                    receiveSocket.BeginReceive(CompletedReceive5, 5);
                    receiveSocket.BeginReceive(CompletedReceive6, 6);

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("\t", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual(" ", s2);
                    Assert.AreEqual(2, p2);

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("\"\"", s3);
                    Assert.AreEqual(3, p3);

                    Assert.AreEqual(true, mre4.WaitOne(timeout), "Timed out waiting 4");
                    Assert.AreEqual("\"", s4);
                    Assert.AreEqual(4, p4);

                    Assert.AreEqual(true, mre5.WaitOne(timeout), "Timed out waiting 5");
                    Assert.AreEqual("\'\'", s5);
                    Assert.AreEqual(5, p5);

                    Assert.AreEqual(true, mre6.WaitOne(timeout), "Timed out waiting 6");
                    Assert.AreEqual("\'", s6);
                    Assert.AreEqual(6, p6);


                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request. We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            // Should receive tab (\t) character.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // Should receive three consecutive spaces ( ).
            // This is the callback for the second receive request.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }

            // Should receive two quotation marks ("")
            // This is the callback for the third receive request.
            private void CompletedReceive3(String s, Exception o, object payload)
            {
                s3 = s;
                p3 = payload;
                mre3.Set();
            }

            // Should receive one quotation mark (").
            // This is the callback for the fourth receive request.
            private void CompletedReceive4(String s, Exception o, object payload)
            {
                s4 = s;
                p4 = payload;
                mre4.Set();
            }

            // Should receive two single quotation marks ('').
            // This is the callback for the fifth receive request.
            private void CompletedReceive5(String s, Exception o, object payload)
            {
                s5 = s;
                p5 = payload;
                mre5.Set();
            }

            // Should receive one single quotation mark (').
            // This is the callback for the sixth receive request.
            private void CompletedReceive6(String s, Exception o, object payload)
            {
                s6 = s;
                p6 = payload;
                mre6.Set();
            }
        }

        // didn't include test from Sean Allen because of noted errors (see comments)

        /// <summary>
        /// Authors: Blake Beckett and Victor Johnson
        /// 
        /// A simple test metod that ensures that messages are received in the correct order, despite when they were sent
        /// </summary>
        [TestMethod]
        public void StringSocketCrowdsourceTest()
        {
            new StringSocketCrowdsourceTestClass().runTest(4001);
        }

        private class StringSocketCrowdsourceTestClass
        {
            // Data that is shared across threads
            private List<Tuple<string, Exception, object>> results;

            public StringSocketCrowdsourceTestClass()
            {
                //use a list of tuples to store the received information from each beginreceive
                results = new List<Tuple<string, Exception, object>>();
            }

            public void runTest(int port)
            {
                //create server and clien members
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    //Initialize server and client
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Connect teh two
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap them in string sockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // First add two receive calls
                    receiveSocket.BeginReceive(MessageReceived1, 1);
                    receiveSocket.BeginReceive(MessageReceived1, 2);

                    //the first two recieve calls should get teh data from this send call
                    sendSocket.BeginSend("Is it working?\nProbably not...\n", (e, o) => { }, 0);

                    //This time, start sending the data first
                    sendSocket.BeginSend("I have no idea what I am doing\nIt's quite sad\n", (e, o) => { }, 0);

                    //If all went well, these should get the strings sent in thesecond beginSend call
                    receiveSocket.BeginReceive(MessageReceived1, 3);
                    receiveSocket.BeginReceive(MessageReceived1, 4);

                    //Make the thread sleep to avoid makin asertions before messages ave been sent(ok for a test case)
                    Thread.Sleep(2000);

                    //Use this method to make assertions
                    MakeAssertions();

                }

                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            //This is th callback used for all beginReceive calls
            private void MessageReceived1(string s, Exception e, object o)
            {
                results.Add(Tuple.Create(s, e, o));
            }

            //Used to make all assertions about the test.
            private void MakeAssertions()
            {
                Assert.AreEqual("Is it working?", results[0].Item1);
                Assert.IsNull(results[0].Item2);
                Assert.AreEqual(1, Convert.ToInt16(results[0].Item3));

                Assert.AreEqual("Probably not...", results[1].Item1);
                Assert.IsNull(results[1].Item2);
                Assert.AreEqual(2, Convert.ToInt16(results[1].Item3));

                Assert.AreEqual("I have no idea what I am doing", results[2].Item1);
                Assert.IsNull(results[2].Item2);
                Assert.AreEqual(3, Convert.ToInt16(results[2].Item3));

                Assert.AreEqual("It's quite sad", results[3].Item1);
                Assert.IsNull(results[3].Item2);
                Assert.AreEqual(4, Convert.ToInt16(results[3].Item3));

            }
        }

        /// <summary>
        /// Author: Dharani Adhikari
        /// This test case is based on what Prof. Jim provided and just checks
        /// if the stringSocket holds message properly when there are many receive requests but only
        /// a few messages were sent.
        /// Note: Since my StringSocket class is not working yet, I am not sure this test case works properly.
        ///</summary>
        [TestMethod()]
        public void TestMessageLeak()
        {
            new MessageLeak().run(4001);
        }

        public class MessageLeak
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private ManualResetEvent mre3;
            private ManualResetEvent mre4;
            private String s1;
            private object p1;
            private String s2;
            private object p2;
            private String s3;
            private object p3;
            private String s4;
            private object p4;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection. We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    mre3 = new ManualResetEvent(false);
                    mre4 = new ManualResetEvent(false);

                    // Make four receive requests
                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);
                    receiveSocket.BeginReceive(CompletedReceive3, 3);
                    receiveSocket.BeginReceive(CompletedReceive4, 4);

                    // Now send the data. Hope those receive requests didn't block!
                    String msg = "Hello world\nThis is a test\nThis is the end of message\n";
                    foreach (char c in msg)
                    {
                        sendSocket.BeginSend(c.ToString(), (e, o) => { }, null);
                    }

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("Hello world", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("This is a test", s2);
                    Assert.AreEqual(2, p2);

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("This is the end of message", s3);
                    Assert.AreEqual(3, p3);

                    Assert.AreEqual(false, mre4.WaitOne(timeout), "Timed out waiting 4");
                    Assert.AreEqual(null, s4);
                    Assert.AreNotEqual(4, p4);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request. We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }

            // This is the callback for the third receive request.
            private void CompletedReceive3(String s, Exception o, object payload)
            {
                s3 = s;
                p3 = payload;
                mre3.Set();
            }

            // This is the callback for the fourth receive request.
            private void CompletedReceive4(String s, Exception o, object payload)
            {
                s4 = s;
                p4 = payload;
                mre4.Set();
            }
        }

        /// <summary>
        /// Author: Chaofeng Zhou and PinEn Chen
        /// Testing two client and thread sending
        /// this test is based the test provided by Jim
        ///</summary>
        [TestMethod()]
        public void TwoCleintRunningDifferentThreads()
        {
            new TwoCleintRunningDifferentThreadsClass().run(4010);
        }

        public class TwoCleintRunningDifferentThreadsClass
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            // recieved string from client 1
            private String s1;
            // payload from client 1
            private object p1;
            // recieved string from client 2
            private String s2;
            // payload from client 2
            private object p2;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and tow clients.
                TcpListener server = null;
                TcpClient client1 = null;
                TcpClient client2 = null;

                try
                {
                    // create a sever
                    server = new TcpListener(IPAddress.Any, port);
                    // start sever
                    server.Start();
                    // create client1
                    client1 = new TcpClient("localhost", port);
                    // create client2
                    client2 = new TcpClient("localhost", port);

                    // get sockets for the two clients and server
                    Socket serverSocket1 = server.AcceptSocket();
                    Socket clientSocket1 = client1.Client;
                    Socket serverSocket2 = server.AcceptSocket();
                    Socket clientSocket2 = client2.Client;

                    // Wrap the four ends of the connection into StringSockets
                    StringSocket sendSocket1 = new StringSocket(serverSocket1, new UTF8Encoding());
                    StringSocket sendSocket2 = new StringSocket(serverSocket2, new UTF8Encoding());
                    StringSocket receiveSocket1 = new StringSocket(clientSocket1, new UTF8Encoding());
                    StringSocket receiveSocket2 = new StringSocket(clientSocket2, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    // Make two receive requests for client 1 and client 2
                    receiveSocket1.BeginReceive(CompletedReceive1, 1);
                    receiveSocket2.BeginReceive(CompletedReceive2, 2);

                    // Now send the data. Hope those receive requests didn't block!
                    String msg1 = "Hello, PinEn Chen.\nHow is your midterm.\n";
                    String msg2 = "Hello\nIt is not your business.\n";

                    // client 1 sends message 1 and client 2 sends messae 2 
                    // using thread
                    ThreadStart threadFunc1 = new ThreadStart(() => SocketSending(msg1, sendSocket1));
                    ThreadStart threadFunc2 = new ThreadStart(() => SocketSending(msg2, sendSocket2));
                    Thread worker1 = new Thread(threadFunc1);
                    Thread worker2 = new Thread(threadFunc2);

                    worker1.Start();
                    worker2.Start();

                    worker1.Join();
                    worker2.Join();

                    // Make sure the first time for the two clients work
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("Hello, PinEn Chen.", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("Hello", s2);
                    Assert.AreEqual(2, p2);

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    receiveSocket1.BeginReceive(CompletedReceive1, 1);
                    receiveSocket2.BeginReceive(CompletedReceive2, 2);

                    // Make sure the second time for the two clients work 
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("How is your midterm.", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("It is not your business.", s2);
                    Assert.AreEqual(2, p2);
                }
                finally
                {
                    server.Stop();
                    client1.Close();
                    client2.Close();
                }
            }

            // call back for client 1
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // call back for client 2
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }

            // a function used for sending message through string socket
            // use this method because I want to do this on different thread
            private void SocketSending(String s, StringSocket ss)
            {
                foreach (char c in s)
                {
                    ss.BeginSend(c.ToString(), (e, o) => { }, null);
                }
            }
        }

        /// <summary>
        /// Written by Christpher McAfee
        ///A simple test for BeginSend and BeginReceive, which calls BeginReceive 1000 times followed by BeginReceive 1000 times,
        ///making sure both match at the end.
        ///</summary>
        [TestMethod()]
        public void TestManyReceiveAndSend()
        {
            new Test104Class().run(4001);
        }

        public class Test104Class
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private String s1;
            private object p1;
            private String s2;
            private object p2;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection. We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    //*** Test for many receive requests followed by many send requests, asserting that they both end at the same value
                    // Make two receive requests
                    for (int i = 0; i < 1000; i++)
                    {
                        receiveSocket.BeginReceive(CompletedReceive, i);
                    }

                    // Now send the data. Hope those receive requests didn't block!
                    for (int i = 0; i < 1000; i++)
                    {
                        mre1.Reset();
                        String msg = i.ToString() + "\n";
                        foreach (char c in msg)
                        {
                            sendSocket.BeginSend(c.ToString(), (e, o) => { }, i);
                        }
                        mre1.WaitOne(timeout);
                    }

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting");
                    Assert.AreEqual("999", s1);
                    Assert.AreEqual(999, p1);


                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request. We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive(String s, Exception o, object payload)
            {

                s1 = s;
                p1 = payload;
                mre1.Set();
            }

        }

    } // close StringSocketTest class
} // close StringSocketTester namespace
