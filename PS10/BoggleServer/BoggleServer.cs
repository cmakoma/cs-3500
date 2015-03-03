﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomNetworking;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using BB;
using System.Timers;
using System.Threading;
using MySql.Data.MySqlClient;

namespace BoggleServer
{
    /// <summary>
    /// This program creates the server side of our Boggle game.
    /// </summary>
    /// <author>Basil Vetas, Lance Petersen</author>
    /// <date>November 18, 2014</date>
    public class BoggleServer
    {
        // Listens for incoming connections
        private TcpListener server;

        // One StringSocket per connected client
        private List<StringSocket> allSockets;        

        // the number of seconds that each Boggle game should last (passed in as parameter)
        private int gameLength;

        // the path name of a file that contains all the legal words (passed in as parameter)
        private HashSet<string> legalWords;

        // optional string of exactly 16 letters used to initialize each Boggle board (optional parameter)
        private string initialBoggleBoard;

        // A player that is waiting to be matched with a new player. 
        private Player waitingPlayer;

        // An object to lock on
        private readonly Object lockObject;

        //-------------PS10 Variables----------------//

        //// increment player ids
        //private int PlayerIDCounter;

        //// increment game ids
        //private int GameIDCounter;

        // web server listener
        private TcpListener webServer;

        //-------------PS10 Variables----------------//
        
        /// <summary>
        /// Will set up the Boggle Server
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // start a new server based on the given arguments and read console line
            new BoggleServer(args);
            Console.ReadLine();
        }

        /// <summary>
        /// Public constructor to check the arguments that are passed in and create a new
        /// BoggleServer based on the given arguments by calling a private constructor. 
        /// </summary>
        /// <param name="args"></param>
        public BoggleServer (string[] args) 
        {          
            // the server can only take two or three arguments
            if ((args.Length != 2) && (args.Length != 3))
            {
                Console.Error.WriteLine("Invalid number of arguments.");
                return;
            }
            
            int time; // holds int for game length                       

            // if the first arg is an int, get it as our timer
            if (int.TryParse(args[0], out time))
            {
                if (!(time > 0))    // if the time is not positive, return with error
                {
                    Console.Error.WriteLine("Invalid time argument. Must be positive.");
                    return;
                }
            }
            else // otherwise return with error
            {
                Console.Error.WriteLine("Invalid time argument. Must be a positive integer.");
                return;
            }

            string pathname = args[1]; // holds path to legal words

            string initBoard; // holds initial board string if provided

            // if there are three parameters, use it
            if (args.Length == 3)
            {
                if (args[2].Length != 16) // if the initial board is not 16 characters
                {
                    // return with error
                    Console.Error.WriteLine("Initial Boggle board setup must have 16 characters.");
                    return;
                }

                if (!(Regex.IsMatch(args[2], @"^[a-zA-Z]+$"))) // if it is not a letter
                {
                    // return with error
                    Console.Error.WriteLine("Initial Boggle board can only be a letter a-z.");
                    return;
                }

                initBoard = args[2];
            }
            else initBoard = null; // otherwise set it to null            

            // initialize BoggleServer private member variables
            this.gameLength = time;            
            this.initialBoggleBoard = initBoard;
            this.waitingPlayer = null;
            this.lockObject = new Object();

            // calls helper methods to initialize the ID counters by 
            // querying the database to find the next available ID
            //PlayerIDCounter = getNextPlayerID();
            //GameIDCounter = getNextGameID();

            string legalWordsFile = pathname;

            // if the pathname is null or empty, return with error
            if (ReferenceEquals(legalWordsFile, null) || legalWordsFile.Equals(""))
            {
                // return with error
                Console.Error.WriteLine("Legal words dictionary cannot be null or empty.");
                return;
            }
            else this.legalWords = getLegalWords(legalWordsFile); // else read in legal words and store        

            // if legalWords is null after getting the words then there was an error so return
            if (ReferenceEquals(legalWords, null))
                return;

            allSockets = new List<StringSocket>(); // will hold all the string sockets between clients            

            server = new TcpListener(IPAddress.Any, 2000); // create new server for boggle
            webServer = new TcpListener(IPAddress.Any, 2500); // create new web server listener

            server.Start(); // start the server

            // begin accepting connections from players
            server.BeginAcceptSocket(ReceivePlayerCallback, null);

            webServer.Start();
            webServer.BeginAcceptSocket(WebRequestCallback, null);
        }

        /// <summary>
        /// Helper method to get the legal words for our Boggle game
        /// </summary>
        /// <param name="pathname"></param>
        /// <returns></returns>
        private HashSet<string> getLegalWords(string pathname)
        {
            HashSet<string> boggleWords = new HashSet<string>();

            try // try to read in the dictionary file
            {
                using (StreamReader reader = new StreamReader(pathname))
                {
                    string word; // holds a legal word

                    // while there is still another word in the file to read
                    while (!(ReferenceEquals(word = reader.ReadLine(), null)))
                    {
                        boggleWords.Add(word);  // add it to our set of legal words
                    }                   
                }
            }
            catch(Exception e) // catch any exception
            {
                Console.WriteLine("The dictionary file could not be read: ");
                Console.WriteLine(e.Message);
                return null;
            }

            return boggleWords; // return list of legal words
        }

        /// <summary>
        /// Deals with connection requests when a new player joins the Boggle game
        /// </summary>
        /// <param name="ar">Resulting status from asynchronous operation</param>
        /// <citation>Chat Server from Lab Examples</citation>
        private void ReceivePlayerCallback(IAsyncResult ar)
        {
            // create a new socket using the received player connection
            try
            {
                Socket socket = server.EndAcceptSocket(ar);
            
                // wrap the socket in a string socket for the new player connection
                StringSocket playerConnection = new StringSocket(socket, UTF8Encoding.Default);
                
                // add it to our list of all the string sockets
                allSockets.Add(playerConnection);

                // let the string socket begin accepting input to read the player name
                playerConnection.BeginReceive(ReceiveNameCallback, playerConnection);

                // let the server accept additional connections from new players
                server.BeginAcceptSocket(ReceivePlayerCallback, null);
            }
            catch { }
        }

        /// <summary>
        /// Receives the first line of text from the client, which contains the name of the new
        /// boggle player.  Uses it to compose and send back a welcome message.
        /// 
        /// Expects a "PLAY @" message after a new user has connected
        /// 
        /// Invariant: the object parameter will always be a String Socket
        /// </summary>
        /// <param name="name">The string input from player</param>
        /// <param name="exception">A possible exception</param>
        /// <param name="payload">A String Socket</param>
        /// <citation>Chat Server from Lab Examples</citation>
        private void ReceiveNameCallback(String name, Exception exception, object payload)
        {
            StringSocket ss = (StringSocket) payload;  // invariant safe to cast as string socket

            // If the name is null or empty, it is invalid
            if (ReferenceEquals(null, name) || name.Equals(""))
            {
                ss.BeginSend("IGNORING\n", (e, o) => { }, ss);
                return;
            }

            // If the reference is non-null, then there was a bad connection.
            if (!ReferenceEquals(null, exception))
            {
                ss.BeginSend("IGNORING " + name + "\n", (e, o) => { }, ss);
                return;
            }

            // make the player name uppercase
            name = name.ToUpper();

            // If name doesn't start with PLAY, return
            if (!(name.StartsWith("PLAY ")))                
            {
                ss.BeginSend("IGNORING " + name + "\n", (e, o) => { }, ss);
                return;
            }
            
            string playerName = name.Substring(5); // use this to store the actual name of the player
            
            lock (lockObject)
            {
                // Create new player using playerName
                Player player = new Player(playerName, ss);                

                if (ReferenceEquals(null, waitingPlayer))
                    waitingPlayer = player;
                else
                {
                    Game game = new Game(waitingPlayer, player, gameLength, initialBoggleBoard, legalWords);
                    
                    game.Start();
                    waitingPlayer = null;
                }
            }
        }

        /// <summary>
        /// Stop the Boggle server from accepting new player connections
        /// </summary>
        public void Stop()
        {
            // socket.close on all sockets
            foreach (StringSocket s in allSockets)
                s.Close();
                
            // stop the server from accepting connections
            server.Stop();
        }

        /// <summary>
        /// Returns the appropriate playerID for the specified player name.
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns>player id</returns>
        //private int getPlayerID(string playerName)
        //{
        //    int playerID = 0;

        //    using (MySqlConnection conn = new MySqlConnection("server=atr.eng.utah.edu;database=cs3500_vetas;uid=cs3500_vetas;password=652097884"))
        //    {
        //        try
        //        {
        //            // Open a connection
        //            conn.Open();

        //            int tempID = 0;

        //            //Check if this player exists in the database
        //            using (MySqlCommand command = new MySqlCommand("SELECT Count(*) FROM PlayerInformation WHERE PlayerName='" + playerName + "';", conn))
        //            {
        //                tempID = Convert.ToInt32(command.ExecuteScalar());
        //            }

        //            //Otherwise get the next available ID for that player
        //            if (tempID == 0)
        //            {
        //                playerID = getNextPlayerID();
        //                PlayerIDCounter++;
        //            }
        //            else
        //            {
        //                using (MySqlCommand command = new MySqlCommand("SELECT PlayerID FROM PlayerInformation WHERE PlayerName='" + playerName + "';", conn))
        //                {
        //                    playerID = Convert.ToInt32(command.ExecuteScalar());
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine(e.Message);
        //        }
        //    }            

        //    //Return the ID
        //    return playerID;
        //}

        /// <summary>
        /// queries database for next available player id
        /// </summary>
        /// <returns>next player id</returns>
        private int getNextPlayerID()
        {
            int nextID = 0;
            using (MySqlConnection conn = new MySqlConnection("server=atr.eng.utah.edu;database=cs3500_vetas;uid=cs3500_vetas;password=652097884"))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // Create the command and get the count
                    using (MySqlCommand command = new MySqlCommand("SELECT COUNT(*) FROM PlayerInformation", conn))
                    {
                        nextID = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            nextID++;
            return nextID;
        }

        /// <summary>
        /// queries database for next available game id
        /// </summary>
        /// <returns>next game id</returns>
        private int getNextGameID()
        {
            int nextID = 0;
            using (MySqlConnection conn = new MySqlConnection("server=atr.eng.utah.edu;database=cs3500_vetas;uid=cs3500_vetas;password=652097884"))
            {
                try
                {
                    // Open a connection
                    conn.Open();                    
                    
                    // Create the command and get the count
                    using (MySqlCommand command = new MySqlCommand("SELECT COUNT(*) FROM GameResults", conn))
                    {                       
                            nextID = Convert.ToInt32(command.ExecuteScalar());                            
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            nextID++;
            return nextID;
        }

        /// <summary>
        /// Callback for requests to web server for game results
        /// </summary>
        /// <param name="ar"></param>
        private void WebRequestCallback(IAsyncResult ar)
        {
            // create a new socket using the received player connection
            try
            {
                Socket webSocket = webServer.EndAcceptSocket(ar);

                // wrap the webSocket in a string socket for the new player connection
                StringSocket webConnection = new StringSocket(webSocket, UTF8Encoding.Default);

                // add it to our list of all the string sockets
                allSockets.Add(webConnection);

                // let the string socket begin accepting input from the web client
                webConnection.BeginReceive(WebMessageCallback, webConnection);

                // let the webServer accept additional connections
                webServer.BeginAcceptSocket(WebRequestCallback, null);
            }
            catch { }
        }

        /// <summary>
        /// Callback for when the web client sends a message through the string socket
        /// 
        /// Invariant: payload will always be the string socket that triggered the callback
        /// </summary>
        /// <param name="name"></param>
        /// <param name="exception"></param>
        /// <param name="payload"></param>
        private void WebMessageCallback(String name, Exception exception, object payload)
        {
            StringSocket ss = (StringSocket) payload; // invariant safe to cast

            string htmlContent = "";

            if (ReferenceEquals(null, name))
            {
                htmlContent = getErrorHTML();
            }
            else if(name.StartsWith("GET /players"))
            {                
                htmlContent = getPlayersHTML();
            } 
            else if(name.StartsWith("GET /games?player="))
            {
                //get the player's name from string and pass as parameter                
                htmlContent = getGamesHTML(name.Substring(18, name.Length-27));
            }
            else if(name.StartsWith("GET /game?id="))
            {
                // get the game id from string and pass as parameter                
                htmlContent = getGameHTML(name.Substring(13, name.Length - 23));
            }
            else 
            {
                htmlContent = getErrorHTML();
            }

            // once we know what kind of message it is, send the html to the client
            ss.BeginSend("HTTP/1.1 200 OK\r\nConnection: close\r\nContent-Type: text/html; charset=UTF-8\r\n\r\n" + htmlContent + "\r\n", (e, o) => { }, ss);
        }       

        /// <summary>
        /// Helper method to get All Players HTML page
        /// </summary>
        /// <returns></returns>
        private string getPlayersHTML()
        {                        
            string playersHTML = "<!DOCTYPE html><html lang=\"en\"> <head><title>Boggle: Games Played</title>"
                    + "<meta charset=\"utf-8\"> <meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge, chrome=1\">"
                    + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
                    + "<meta name=\"description\" content=\"Boggle: Games Played\"> <style>" + addCSS() + "</style>"
                    + "</head> <body> <div id=\"wrapper\"> <header> <h1>Boggle: Games Played</h1> </header>"
                    + "<div id=\"main\"> <p>All Boggle Games</p> <table> <tr class=\"title\"> <td>Player</td> <td>Games Won</td>"
                    + "<td>Games Lost</td> <td>Games Tied</td> </tr>";           

            using (MySqlConnection conn = new MySqlConnection("server=atr.eng.utah.edu;database=cs3500_vetas;uid=cs3500_vetas;password=652097884"))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    //Get the playerName specified.
                    string playerName;                    

                    // for each ID in the database, get the player name
                    for(int i = 1; i < getNextPlayerID(); i++)
                    {
                        int wins = 0;
                        int losses = 0;
                        int ties = 0;

                        using (MySqlCommand command = new MySqlCommand("SELECT PlayerName FROM PlayerInformation WHERE PlayerID='" + i + "';", conn))
                        {
                            playerName = (string) command.ExecuteScalar(); // get name as a string                                                        
                        }

                        int score1 = 0;
                        int score2 = 0;

                        // Create a command
                        MySqlCommand command10 = conn.CreateCommand();
                        command10.CommandText = "SELECT PlayerOneScore,PlayerTwoScore FROM GameResults WHERE PlayerOneID='" + i + "';";

                        // Execute the command and cycle through the DataReader object
                        using (MySqlDataReader reader = command10.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                score1 = Convert.ToInt32(reader["PlayerOneScore"]);
                                score2 = Convert.ToInt32(reader["PlayerTwoScore"]);

                                // figure out if the player won, lost or tied
                                if (score1 > score2) wins++;
                                else if (score1 < score2) losses++;
                                else ties++;
                            }                              
                        }
    
                        score1 = 0;
                        score2 = 0;

                        // Create a command
                        MySqlCommand command11 = conn.CreateCommand();
                        command11.CommandText = "SELECT PlayerOneScore,PlayerTwoScore FROM GameResults WHERE PlayerTwoID='" + i + "';";

                        // Execute the command and cycle through the DataReader object
                        using (MySqlDataReader reader = command11.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                score1 = Convert.ToInt32(reader["PlayerOneScore"]);
                                score2 = Convert.ToInt32(reader["PlayerTwoScore"]);

                                // figure out if the player won, lost or tied
                                if (score1 > score2) losses++;
                                else if (score1 < score2) wins++;
                                else ties++;
                            }                                
                        }
                                                                      
                        // add a new row to our string of HTML with the appropriate data
                        playersHTML += "<tr> <td><a href=\"games?player=" + playerName + "\">" + playerName + "</a></td>"
                        + "<td>" + wins + "</td> <td>" + losses + "</td> <td>" + ties + "</td> </tr> ";                                                        
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            // close the table and add footer before returning string of HTML
            playersHTML += " </table> </div> </div> <footer> <p> Boggle Game Results by Lance Petersen and Basil Vetas </p> </footer> </body> </html>";
             
            return playersHTML;
        }

        /// <summary>
        /// Helper method to get Single Player Games HTML page
        /// </summary>
        /// <returns></returns>
        private string getGamesHTML(string playerName)
        {
            string gameHTML = "<!DOCTYPE html><html lang=\"en\"><head><title>Boggle: Player Statistics</title><meta charset=\"utf-8\">"
            + "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge, chrome=1\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
            + "<meta name=\"description\" content=\"Boggle: Player Statistics\"><style>" + addCSS() + "</style></head>"
            + "<body><div id=\"wrapper\">  <header><h1>Boggle: Player Statistics</h1></header><div id=\"main\"><p>" + playerName + "</p>"
            + "<h6><a href=\"players\">Back to Home</a></h6><table><tr class=\"title\"><td>Game Number</td><td>Date/time</td><td>Player</td><td>Opponent</td>"
            + "<td>Player Score</td><td>Opponent Score</td></tr>";

            using (MySqlConnection conn = new MySqlConnection("server=atr.eng.utah.edu;database=cs3500_vetas;uid=cs3500_vetas;password=652097884"))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // Get the playerID specified.
                    int playerID;

                    using (MySqlCommand command = new MySqlCommand("SELECT PlayerID FROM PlayerInformation WHERE PlayerName='" + playerName + "';", conn))
                    {
                        playerID = Convert.ToInt32(command.ExecuteScalar()); // get playerID as an int                                                        
                    }

                    int gameID = 0;
                    string dateAndTime = "";
                    int oppID = 0;
                    string opponent = "";
                    int playerScore = 0;
                    int oppScore = 0;

                    // Create a command
                    MySqlCommand command1 = conn.CreateCommand();
                    command1.CommandText = "SELECT * FROM GameResults INNER JOIN PlayerInformation ON GameResults.PlayerOneID='" + playerID + "' and PlayerInformation.PlayerID=GameResults.PlayerTwoID;";

                    // Execute the command and cycle through the DataReader object
                    using (MySqlDataReader reader1 = command1.ExecuteReader())
                    {
                        while (reader1.Read())
                        {
                            gameID = Convert.ToInt32(reader1["GameID"]);
                            dateAndTime = (string)reader1["DateAndTime"];                            
                            playerScore = Convert.ToInt32(reader1["PlayerOneScore"]);
                            oppScore = Convert.ToInt32(reader1["PlayerTwoScore"]);
                            opponent = (string)reader1["PlayerName"];

                            gameHTML += "<tr><td><a href=\"game?id=" + gameID + "\">" + gameID + "</a></td><td>" + dateAndTime + "</td><td>"
                            + "<a href=\"games?player=" + playerName + "\">" + playerName + "</a></td><td><a href=\"games?player=" + opponent + "\">" + opponent + "</a></td>"
                            + "<td>" + playerScore + "</td><td>" + oppScore + "</td>";
                        }
                    }                                       

                    gameID = 0;
                    dateAndTime = "";
                    oppID = 0;
                    opponent = "";
                    playerScore = 0;
                    oppScore = 0;

                    // Create a command
                    MySqlCommand command2 = conn.CreateCommand();
                    command2.CommandText = "SELECT * FROM GameResults INNER JOIN PlayerInformation ON GameResults.PlayerTwoID='" + playerID + "' and PlayerInformation.PlayerID=GameResults.PlayerOneID;";

                    // Execute the command and cycle through the DataReader object
                    using (MySqlDataReader reader2 = command2.ExecuteReader())
                    {
                        while (reader2.Read())
                        {
                            gameID = Convert.ToInt32(reader2["GameID"]);
                            dateAndTime = (string)reader2["DateAndTime"];                            
                            playerScore = Convert.ToInt32(reader2["PlayerTwoScore"]);
                            oppScore = Convert.ToInt32(reader2["PlayerOneScore"]);
                            opponent = (string)reader2["PlayerName"];

                            gameHTML += "<tr><td><a href=\"game?id=" + gameID + "\">" + gameID + "</a></td><td>" + dateAndTime + "</td><td>"
                            + "<a href=\"games?player=" + playerName + "\">" + playerName + "</a></td><td><a href=\"games?player=" + opponent + "\">" + opponent + "</a></td>"
                            + "<td>" + playerScore + "</td><td>" + oppScore + "</td>"; 
                        }
                    }                                         
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            gameHTML += "</tr></table></div></div><footer><p>Boggle Game Results by Lance Petersen and Basil Vetas</p> </footer></body></html>";

            return gameHTML;                               
        }

        /// <summary>
        /// Helper method to get Single Game HTML page
        /// </summary>
        /// <returns></returns>
        private string getGameHTML(string gameID)
        {
            string gameHTML = "<!DOCTYPE html><html lang=\"en\"><head><title>Boggle: Game Statistics</title>"
            + " <meta charset=\"utf-8\"><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge, chrome=1\">"
            + " <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><meta name=\"description\" content=\"Boggle: Game Statistics\">"
            + " <style>" + addCSS() + "</style></head><body><div id=\"wrapper\">  <header><h1>Boggle: Game Statistics</h1>"
            + " </header><div id=\"main\"><p>Game Number "+ gameID + " Results:</p><h6><a href=\"players\">Back to Home</a></h6>"
			+ " <table><tr class=\"title\"><td>Player One</td><td>Score One</td><td>Player Two</td><td>Score Two</td><td>Date/time</td></tr>";

            using (MySqlConnection conn = new MySqlConnection("server=atr.eng.utah.edu;database=cs3500_vetas;uid=cs3500_vetas;password=652097884"))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // data that we need from database
                    string playerOne = "";
                    string playerTwo = "";
                    int idOne = 0;
                    int idTwo = 0;
                    int scoreOne = 0;
                    int scoreTwo = 0;
                    string dateAndTime = "";
                    string boggleboard = "";
                    char[] b = new char[16];

                    MySqlCommand command = conn.CreateCommand();
                    command.CommandText = "SELECT PlayerOneID,PlayerTwoID,PlayerOneScore,PlayerTwoScore,DateAndTime,BoggleBoard FROM GameResults WHERE GameID='" + gameID + "';";

                    // Execute the command and cycle through the DataReader object
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            idOne = Convert.ToInt32(reader["PlayerOneID"]);
                            idTwo = Convert.ToInt32(reader["PlayerTwoID"]);
                            scoreOne = Convert.ToInt32(reader["PlayerOneScore"]);
                            scoreTwo = Convert.ToInt32(reader["PlayerTwoScore"]);
                            dateAndTime = (string)reader["DateAndTime"];
                            boggleboard = (string)reader["BoggleBoard"];
                        }
                    }

                    MySqlCommand command2 = conn.CreateCommand();
                    command2.CommandText = "SELECT PlayerName FROM PlayerInformation WHERE PlayerID='" + idOne + "';";

                    // get player one name using the ID
                    using (MySqlDataReader reader = command2.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            playerOne = (string)reader["PlayerName"];
                        }
                    }

                    MySqlCommand command3 = conn.CreateCommand();
                    command3.CommandText = "SELECT PlayerName FROM PlayerInformation WHERE PlayerID='" + idTwo + "';";

                    // get player two name using the ID
                    using (MySqlDataReader reader = command3.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            playerTwo = (string)reader["PlayerName"];
                        }
                    }

                    // put string into a char array
                    b = boggleboard.ToCharArray(0, 16);

                    gameHTML += "<tr><td><a href=\"games?player=" + playerOne + "\">" + playerOne + "</a></td><td>" + scoreOne + "</td><td><a href=\"games?player=" + playerTwo + "\">" + playerTwo + "</a></td>"
                    + "<td>" + scoreTwo + "</td><td>" + dateAndTime + "</td></tr></table><p>Boggle Board</p> "
                    + "<table class=\"boggle-board\"><tr><td>" + b[0] + "</td><td>" + b[1] + "</td><td>" + b[2] + "</td><td>" + b[3] + "</td></tr><tr><td>" + b[4] + "</td><td>" + b[5] + "</td><td>" + b[6] + "</td><td>" + b[7] + "</td></tr><tr>"
                    + "<td>" + b[8] + "</td><td>" + b[9] + "</td><td>" + b[10] + "</td><td>" + b[11] + "</td></tr><tr><td>" + b[12] + "</td><td>" + b[13] + "</td><td>" + b[14] + "</td><td>" + b[15] + "</td></tr>";

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            gameHTML += "</table></div></div><footer><p>Boggle Game Results by Lance Petersen and Basil Vetas</p> </footer></body></html>";
            return gameHTML;
        }

        /// <summary>
        /// Helper method to get Error HTML page
        /// </summary>
        /// <returns></returns>
        private string getErrorHTML()
        {
            return "<!DOCTYPE html> <html lang=\"en\"> 	<head> <title>Error: Page Not Found</title>"
                + "<meta charset=\"utf-8\"> <meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge, chrome=1\">"
                + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"> <meta name="
                + "\"description\" content=\"Error: Page Not Found\"> <style>" + addCSS() + "</style> </head> <body> <div id=\"wrapper\"> <header> <h1>Error: Page Not Found</h1>"
                + "</header> <div id=\"main\"> <p>Your web request is invalid or the page does not exist.  Valid requests "
                + "are of the form:</p> <ul> <li>/players</li> <li>/games?player=player_name </li>"
                + "<li>/game?id=game_id</li> </ul> <h6><a href=\"players\">Back to Home</a></h6> </div> </div> <footer> <p> Boggle Game Results by Lance "
                + "Petersen and Basil Vetas</p> </footer> </body> </html>";
        }

        /// <summary>
        /// Helper method to add CSS style to html pages above
        /// </summary>
        /// <returns></returns>
        private string addCSS()
        {
            return "* {-moz-box-sizing: border-box; -webkit-box-sizing: border-box; box-sizing: border-box;}"
                + "html {font-size: 10px; height: 100%;} @media (max-width: 768px) { html { font-size: 8px; height: 100%;} }"
                + "@media (max-width: 480px) { html { font-size: 6px; height: 100%;}}" 
                + " body {  font-family: 'Lato', sans-serif;line-height: 1.5; background: url(white); height: 100%; background-size: 100% 100%;"
                + "background-position: center;background-attachment: fixed;} #wrapper {min-height: 100%;margin-bottom: -5rem; }"
                + "#wrapper:after {content: \"\"; display: block;height: -5rem;} p {font-size: 1.4rem;} "
                + "h6 {  font-size: 1.2rem;margin-top: 0;  } ul {padding: 0;} ul li {list-style: none;} "
                + "table {font-size: 1.4rem;} a {  color: #000000;text-decoration: none;} a:hover {color: blue;}" 
                + "header {  background: #552448;padding: 1rem 4rem;width: 100%;float: left;color: #FFFFFF;text-align: center;}"
                + "h1 {  font-size: 2rem;font-family: georgia, times, serif; } #main {float: left;padding: 1rem 4rem;color: #000000;}"
                + "#main p {font-weight: bold;text-decoration: underline;font-size: 2rem;margin-bottom: 0;}"
                + ".title {text-decoration: underline; }tr {text-align: center;}table.boggle-board tr td {padding-right: 2rem;}"
                + "footer {border-top: .1em solid #DDDDDD;height: 5rem;width: 100%;background: #552448;float: left;padding: 1rem 4rem;color: #FFFFFF;}"
                + "footer p {text-align: center;}";
        }        
    }

    /// <summary>
    /// A new Boggle player
    /// </summary>
    public class Player
    {
        /// <summary>
        /// 
        /// </summary>
        public string name { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public StringSocket ss {get; private set;}

        /// <summary>
        /// 
        /// </summary>
        public int score {get; set;}

        /// <summary>
        /// 
        /// </summary>
        public HashSet<string> legalWordsFound { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public HashSet<string> illegalWordsFound { get; private set; }


        /// <summary>
        /// Constructor for a new player
        /// </summary>
        public Player(string playerName, StringSocket connection)
        {
            // initialize variables
            name = playerName;
            ss = connection;
            score = 0;
            legalWordsFound = new HashSet<string>();
            illegalWordsFound = new HashSet<string>();
        }
    }

    /// <summary>
    /// A new Boggle game
    /// </summary>
    public class Game
    {
        // private memeber variables
        private Player playerOne;
        private Player playerTwo;
        private int timeRemaining;
        private System.Timers.Timer timer; 
        private BoggleBoard board;
        private HashSet<string> dictionary;
        private HashSet<string> commonWords;

        /// <summary>
        /// The string used to connect to the database
        /// </summary>
        private string connectionString = "server=atr.eng.utah.edu;database=cs3500_vetas;uid=cs3500_vetas;password=652097884";


        /// <summary>
        /// 
        /// </summary>
        public int gameLength { get; private set; }

        /// <summary>
        /// Constructor for a new game of boggle
        /// </summary>
        /// <param name="playerOne"></param>
        /// <param name="playerTwo"></param>
        /// <param name="gameLength"></param>
        /// <param name="initialBoardSetup"></param>
        /// <param name="legalWordsDictionary"></param>
        public Game(Player playerOne, Player playerTwo, int gameLength, string initialBoardSetup, HashSet<string> legalWordsDictionary)
        {
            //Initialize the game.
            if (ReferenceEquals(initialBoardSetup, null))
                board = new BoggleBoard();
            else
                board = new BoggleBoard(initialBoardSetup);

            this.playerOne = playerOne;
            this.playerTwo = playerTwo;
            this.timeRemaining = gameLength;
            this.timer = new System.Timers.Timer(1000);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            this.dictionary = legalWordsDictionary;
            this.commonWords = new HashSet<string>();
            this.gameLength = gameLength;
        }

        /// <summary>
        /// Every time 1 second has elapses, decrement the timeRemaining and 
        /// send the time to both clients.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            lock (timer)
            {
                //decrement time
                timeRemaining--;

                //Send the time remaining to each client
                string message = "TIME " + timeRemaining + "\n";
                playerOne.ss.BeginSend(message, (ex, o) => { }, null);
                playerTwo.ss.BeginSend(message, (ex, o) => { }, null);
                
                //When time has run out
                if (timeRemaining < 1)
                {
                    //Stop timer
                    timer.Stop();

                    //Send final scores
                    playerOne.ss.BeginSend("SCORE " + playerOne.score + " " + playerTwo.score + "\n", (o, ex) => { }, null);
                    playerTwo.ss.BeginSend("SCORE " + playerTwo.score + " " + playerOne.score + "\n", (o, ex) => { }, null); 

                    //Send the summaries of the game.
                    playerOne.ss.BeginSend(MakeGameSummary(playerOne, playerTwo), (o, ex) => { }, null);
                    playerTwo.ss.BeginSend(MakeGameSummary(playerTwo, playerOne), (o, ex) => { }, null);
                    updateDatabase(playerOne, playerTwo, gameLength, board.ToString());

                    //Close the sockets
                    playerOne.ss.Close();
                    playerTwo.ss.Close();
                }
            }
        }

        /// <summary>
        /// Starts the game of Boggle
        /// </summary>
        public void Start()
        {
            //Send the START command to both players. 
            playerOne.ss.BeginSend("START " + board.ToString() + " " + timeRemaining + " " + playerTwo.name + "\n", (e, o) => { }, playerOne.ss);
            playerTwo.ss.BeginSend("START " + board.ToString() + " " + timeRemaining + " " + playerOne.name + "\n", (e, o) => { }, playerTwo.ss);
            playGame();
        }

        /// <summary>
        /// The actual gameplay for a Boggle game
        /// </summary>
        private void playGame ()
        {
            //Start the timer.
            timer.Start();

            // receive words from the the players as they find them on the board
            playerOne.ss.BeginReceive(receiveWordsCallback, playerOne);
            playerTwo.ss.BeginReceive(receiveWordsCallback, playerTwo);                
        }

        /// <summary>
        /// Callback for when a player finds a word (or thinks they have found a word)
        /// 
        /// Invariant: the third parameter should always be a Player object
        /// </summary>
        /// <param name="word">The word found</param>
        /// <param name="exception">Possible exception</param>
        /// <param name="payload">The player who found the word</param>
        private void receiveWordsCallback(string word, Exception exception, object payload)
        {
            lock (board)
            {
                // player one and two, depending on who found the word
                Player playerFoundWord = payload as Player;
                Player opponent;

                // if the player who found the word is playerOne, then the opponent must be playerTwo
                if (ReferenceEquals(playerFoundWord, playerOne))
                    opponent = playerTwo;
                else opponent = playerOne;  // otherwise the opponent is playerOne

                // If both the word and the exception are null, then the socket has been closed.
                // Send "TERMINATED" to the remaining socket and then close that socket. 
                if (ReferenceEquals(null, word))
                {
                    opponent.ss.BeginSend("TERMINATED\n", (e, o) => { }, null);
                    opponent.ss.Close();
                    return;
                }

                // helper method to update the player scores
                updateScore(word, playerFoundWord, opponent);

                //send updated scores to players
                playerFoundWord.ss.BeginSend("SCORE " + playerFoundWord.score + " " + opponent.score + "\n", (o, e) => { }, null);
                opponent.ss.BeginSend("SCORE " + opponent.score + " " + playerFoundWord.score + "\n", (o, e) => { }, null);

                // receive more words from the player 
                playerFoundWord.ss.BeginReceive(receiveWordsCallback, playerFoundWord);
            }
        }

        /// <summary>
        /// Helper method to update the score when a player finds a word
        /// 
        /// Invariant: The second paramter should always be the player who found the word
        /// and the third parameter should always be the opponent 
        /// </summary>
        /// <param name="word">The word found</param>
        /// <param name="playerFoundWord">Player who found it</param>
        /// <param name="opponent">Opponent player</param>
        private void updateScore(string word, Player playerFoundWord, Player opponent)
        {                       
            // make the message all uppercase
            word = word.ToUpper();

            // If name doesn't start with WORD
            if (!(word.StartsWith("WORD ")))
            {
                // then ignore the command and return
                playerFoundWord.ss.BeginSend("IGNORING\n", (e, o) => { }, playerOne.ss);
                return;
            }

            word = word.Substring(5);

            // if word is less than three characters, don't count it
            if (word.Length < 3)
                return;

            // if the word is not valid
            if (!(dictionary.Contains(word)) || !board.CanBeFormed(word))
            {
                // remove a point from the play who found the work
                playerFoundWord.score--;
                playerFoundWord.illegalWordsFound.Add(word);
            }
            else // otherwise if the word is valid
            {
                if (opponent.legalWordsFound.Contains(word)) // then if opponent already found the word
                {
                    // then remove the word from opponents's list                        
                    opponent.legalWordsFound.Remove(word);

                    // and add it to the list of common words
                    commonWords.Add(word);

                    // and reduce opponent's score by the value of word
                    opponent.score = opponent.score - getValue(word);
                }
                else // if the opponent hasn't found the word
                {
                    // then if the finder hasn't found the word yet either
                    if (!(playerFoundWord.legalWordsFound.Contains(word)))
                    {
                        // then add the word to player one's list                        
                        playerFoundWord.legalWordsFound.Add(word);

                        // and increase player one's score by the value of word
                        playerFoundWord.score = playerFoundWord.score + getValue(word);
                    }
                }
            }
            
        }

        /// <summary>
        /// Helper method to get the point value of a found word.
        /// 
        /// Invariant: word will never be less than 3 characters
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        private int getValue(string word)
        {
            int pointValue = 0; // holds the value of word

            // if word has 3 or 4 characters
            if ((word.Length == 3) || word.Length == 4)
                pointValue = 1; // worth one point
            else if (word.Length == 5) // if 5 characters
                pointValue = 2; // worth 2 point
            else if (word.Length == 6) // if 6 characters 
                pointValue = 3; // worth 3 points
            else if (word.Length == 7) // if 7 characters
                pointValue = 5; // worth 5 points
            else // if more than 7 charcters
                pointValue = 11; // worth 11 points

            return pointValue;
        }

        /// <summary>
        /// Generate a summary of the game for the player specified.
        /// Add the outcome of the game to a database. 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="opponent"></param>
        /// <returns></returns>
        private string MakeGameSummary(Player player, Player opponent)
        {
            string summary = "STOP ";

            //how many words this player found
            summary += player.legalWordsFound.Count + " ";

            foreach (string s in player.legalWordsFound)
            {
                //all the unique, legal words
                summary += s + " ";
            }

            //how many words opponent found
            summary += opponent.legalWordsFound.Count + " ";

            //all of the opponent's words
            foreach (string s in opponent.legalWordsFound)
            {
                summary += s + " ";
            }

            //how many legal words in common
            summary += commonWords.Count + " ";

            //list
            foreach (string s in commonWords)
            {
                summary += s + " "; 
            }

            //how many illegal words
            summary += player.illegalWordsFound.Count + " ";

            //list
            foreach (string s in player.illegalWordsFound)
            {
                summary += s + " ";
            }

            //how many opponent illegal words
            summary += opponent.illegalWordsFound.Count + " ";

            //list
            foreach (string s in opponent.illegalWordsFound)
            {
                summary += s + " ";
            }

            summary += "\n";
            return summary;
        }

        /// <summary>
        /// Adds the summary information to the database
        /// </summary>
        /// <param name="playerOne"></param>
        /// <param name="playerTwo"></param>
        /// <param name="?"></param>
        /// <param name="gameLength"></param>
        /// <param name="board"></param>
        private void updateDatabase(Player playerOne, Player playerTwo, int gameLength, string board)
        {                    
            //Need to add names and final scores of the two players, date and time when game ended, 
            //the time limit that was used in the game, the board, and the 5 part word summary.
            string name1 = playerOne.name; 
            string name2 = playerTwo.name;            
            int score1 = playerOne.score;
            int score2 = playerTwo.score;
            string dateAndTime = DateTime.Now.ToLocalTime().ToString();            
            int timeLimit = gameLength;
            string boardLetters = board.ToString();            

            //Now add this stuff to a database.      
            int id1 = updatePlayerInformation(name1);
            int id2 = updatePlayerInformation(name2);
            int gameid = updateGameResults(id1, id2, dateAndTime, boardLetters, timeLimit, score1, score2);
            
            // Note: thr fourth parameter below is 0 if false and 1 if true

            // add the legal words from player 1 to database
            foreach(string s in playerOne.legalWordsFound)
                updateWordsPlayed(s, gameid, id1, 1);

            // add the legal words from player 2 to database
            foreach(string s in playerTwo.legalWordsFound)
                updateWordsPlayed(s, gameid, id2, 1);

            // add the illegal words from player 1 to database
            foreach(string s in playerOne.illegalWordsFound)
                updateWordsPlayed(s, gameid, id1, 0);

            // add the illegal words from player 2 to database
            foreach(string s in playerTwo.illegalWordsFound)
                updateWordsPlayed(s, gameid, id2, 0);

            // add the common words to database for each player
            foreach(string s in commonWords)
            {
                updateWordsPlayed(s, gameid, id1, 1);
                updateWordsPlayed(s, gameid, id2, 1);
            }
        }        

        /// <summary>
        /// helper method to update PlayerInformation table (adds data for a single player)
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns>true if the update was successful, false if not</returns>
        private int updatePlayerInformation(string playerName)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    int tempID = 0;

                    //Check if this player exists in the database
                    using (MySqlCommand command1 = new MySqlCommand("SELECT Count(*) FROM PlayerInformation WHERE PlayerName='" + playerName + "';", conn))
                    {
                        tempID = Convert.ToInt32(command1.ExecuteScalar());
                    }

                    // if temp is zero then name is not in database so add it
                    if (tempID == 0)
                    {
                        // Create a command
                        MySqlCommand command = conn.CreateCommand();
                        command.CommandText = "INSERT INTO PlayerInformation (PlayerName) VALUES ('" + playerName + "'); ";
                        command.ExecuteNonQuery();
                    }                                           
                   
                    using (MySqlCommand command1 = new MySqlCommand("SELECT PlayerID FROM PlayerInformation WHERE PlayerName='" + playerName + "';", conn))
                    {
                        return Convert.ToInt32(command1.ExecuteScalar());
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -1;
                }
            }
        }

        /// <summary>
        /// helper method to update GameResults table (adds data from a single game)
        /// </summary>
        /// <param name="board"></param>
        /// <param name="dateTime"></param>
        /// <param name="playerOneID"></param>
        /// <param name="playerTwoID"></param>
        /// <param name="scoreOne"></param>
        /// <param name="scoreTwo"></param>
        /// <param name="timeLimit"></param>
        /// <returns>true if the update was successful, false if not</returns>
        private int updateGameResults(int playerOneID, int playerTwoID, string dateTime, string board, int timeLimit, int scoreOne, int scoreTwo)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // Create a command
                    MySqlCommand command = conn.CreateCommand();
                    command.CommandText = "INSERT INTO GameResults (PlayerOneID, PlayerTwoID, DateAndTime, BoggleBoard, TimeLimit, PlayerOneScore, PlayerTwoScore) VALUES ('" + playerOneID + "', '" + playerTwoID + "', '" + dateTime + "', '" + board + "', '" + timeLimit + "', '" + scoreOne + "', '" + scoreTwo + "' ); ";
                    command.ExecuteNonQuery();

                    using (MySqlCommand command1 = new MySqlCommand("SELECT Count(*) FROM GameResults;", conn))
                    {
                        return Convert.ToInt32(command1.ExecuteScalar());
                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -1;
                }
            }
        }

        /// <summary>
        /// helper method to update WordsPlayed table (only updates a single word found in a game)
        /// 
        /// Invariant: the fourth parameter should always be either equal to 1 (true) or 0 (false)
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="isLegal"></param>
        /// <param name="playerID"></param>
        /// <param name="word"></param>
        /// <returns>true if the update was successful, false if not</returns>
        private bool updateWordsPlayed(string word, int gameID, int playerID, int isLegal )
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // Create a command
                    MySqlCommand command = conn.CreateCommand();
                    command.CommandText = "INSERT INTO WordsPlayed (Word, GameID, PlayerID, Legal) VALUES ('" + word + "', '" + gameID + "', '" + playerID + "', '" + isLegal + "' ); ";
                    command.ExecuteNonQuery();
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
        }
    }
}
