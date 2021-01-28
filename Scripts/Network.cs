﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

public class Network
{
    public static Network network;

    public static bool isConnected = false;

    private static Message messageAvailable = null;

    public static int PlayerID = -1;
    public static string name = "";

    public static IDMessage lastMessage = IDMessage.NONE;

    public IPHostEntry host = Dns.GetHostEntry("localhost");
    public IPAddress ipAddress;
    public IPEndPoint remoteEP;

    private Socket sender;

    private byte[] buffer = new byte[32096];
    public static UIController controller { get; set; }
    public static Matrix matrix { get; set; }

    private string receivedMatrix;

    public static Dictionary<string, Fighter> warriors = new Dictionary<string, Fighter>();

    private void Start()
    {
        try
        {
            ipAddress = host.AddressList[0];
            remoteEP = new IPEndPoint(ipAddress, 42069);

            // Create a TCP/IP  socket.    
            sender = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.    
            try
            {
                // Connect to Remote EndPoint  
                sender.Connect(remoteEP);

                Debug.Log("Socket connected to " + sender.RemoteEndPoint.ToString());

                isConnected = true;

                Thread listener = new Thread(new ThreadStart(Listener));

                listener.Start();

            }
            catch (ArgumentNullException ane)
            {
                Debug.Log("ArgumentNullException :" +  ane.ToString());
            }
            catch (SocketException se)
            {
                Debug.Log("SocketException :" + se.ToString());
            }
            catch (Exception e)
            {
                Debug.Log("Unexpected exception :" + e.ToString());
            }

        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    // Listener de mensajes
    private void Listener()
    {
        while (isConnected)
        {
            Array.Clear(buffer, 0, buffer.Length);

            int bytes = sender.Receive(buffer);

            if (bytes <= 0) continue;

            Debug.Log("Cantidad de bytes: " + bytes.ToString());

            string received = Encoding.UTF8.GetString(buffer);

            Debug.Log("JSON: " + received);

            Message message = JsonConvert.DeserializeObject<Message>(received);

            messageAvailable = message;

            if (messageAvailable != null)
            {
                OnMessageReceived();
            }
        }
    }

    private void OnMessageReceived()
    {
        IDMessage id = Utils.getMessage(messageAvailable.idMessage);

        lastMessage = id;

        Debug.Log(messageAvailable.idMessage + " : " + messageAvailable.text);

        Message message;

        switch (id)
        {
            case IDMessage.MESSAGE:
                controller.AddChatMessage(messageAvailable.text);
                break;
            case IDMessage.LOGBOOK:
                controller.AddLogbookMessage(messageAvailable.text);
                break;
            case IDMessage.ACCEPTED:
                message = new Message
                {
                    idMessage = "DONE"
                };

                SendMessage(message);

                break;
            case IDMessage.STARTED:
                // TODO: El mero desvergue de cambiar de scene
                Debug.Log("El juego puede iniciar!");

                LoginController.ChangeScene = true;

                break;
            case IDMessage.REQUESTNAME:
                Debug.Log("Respondiendo el nombre...");

                message = new Message
                {
                    text = name,
                    idMessage = "RESPONSE" // TODO: Cambiar esto en utils
                };

                SendMessage(message);

                break;
            case IDMessage.REQUESTCHARACTERS:
                Debug.Log("Enviando luchadores");

                // TODO: Mover a utils
                string[] warriorText = new string[3];

                for (int i = 0; i < 3; i++)
                    warriorText[i] = JsonConvert.SerializeObject(FighterGenerator.GetFighters()[i]);

                message = new Message
                {
                    id = PlayerID,
                    texts = warriorText,
                    idMessage = "RESPONSE" // TODO: Cambiar esto en utils :D
                };

                SendMessage(message);

                break;
            case IDMessage.ID:
                PlayerID = messageAvailable.number;
                Debug.Log("ID del jugador: " + PlayerID.ToString());

                message = new Message
                {
                    idMessage = "DONE"
                };

                SendMessage(message);

                break;
            case IDMessage.ADMIN:
                Debug.Log("Admin");
                Message admin = new Message
                {
                    number = 2, // TODO: Cambiar esto a un combo box
                    idMessage = "RESPONSE"
                };

                SendMessage(admin);

                break;
            case IDMessage.REJECTED:
                Debug.Log("F");
                // TODO: Trigger mensaje global
                Disconnect();
                break;
            // Mensajes una vez iniciado el juego
            case IDMessage.INITMATRIX1:
                Debug.Log("Primera parte de la matriz...");

                receivedMatrix = messageAvailable.text;

                message = new Message
                {
                    idMessage = "DONE"
                };

                SendMessage(message);

                break;
            case IDMessage.INITMATRIX2:
                receivedMatrix += messageAvailable.text;

                Debug.Log("Matriz completamente recibida...");
                Debug.Log("Matriz: \n" + receivedMatrix);

                string[,] result = JsonConvert.DeserializeObject<string[,]>(receivedMatrix);

                Matrix.SetMatrix(result);

                message = new Message
                {
                    idMessage = "DONE"
                };

                SendMessage(message);

                break;
            case IDMessage.INTMATRIX:
                Debug.Log("Digale a Juan que parsee esto a una matriz de enteros para habilitar la matriz...");

                message = new Message
                {
                    idMessage = "DONE"
                };

                SendMessage(message);
                break;
            case IDMessage.GETFIGHTER:
                Debug.Log("Digale a Juan que agregue la funcion aqui para agregarlo a la UI...");

                message = new Message
                {
                    idMessage = "DONE"
                };

                SendMessage(message);
                break;
            default:
                Debug.Log("Mensaje no soportado: " + messageAvailable.idMessage);

                message = new Message
                {
                    idMessage = "DONE"
                };

                SendMessage(message);

                break;
        }

        messageAvailable = null;
    }

    public void SendMessage(Message message)
    {
        string json = JsonConvert.SerializeObject(message);

        sender.Send(Encoding.UTF8.GetBytes(json));
    }

    public void Disconnect()
    {
        sender.Shutdown(SocketShutdown.Both);
        sender.Close();

        isConnected = false;
    }

    public static Network getInstance()
    {
        if (network == null)
        {
            network = new Network();

            network.Start();
        }

        return network;
    }
}