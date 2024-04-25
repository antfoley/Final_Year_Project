using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PimDeWitte.UnityMainThreadDispatcher;
using boxClasses;

public class newStickFigureScript : MonoBehaviour
{
    private string latestMessage1 = "";
    private string latestMessage2 = "";

    private UdpClient udpListener1;
    private UdpClient udpListener2;
    private bool isServerRunning = false;
    private BoxManager boxManager = new BoxManager();
    private bool firstMessage;
    private GameObject box;
    private int counter;
    private int totalCounter;
    private float timeElapsed;
    private float totalElapsedTime;

    private void Start()
    {
        isServerRunning = true;
        firstMessage = true;
        udpListener1 = new UdpClient(12345);
        udpListener2 = new UdpClient(12346);
        Debug.Log("UDP Server is running");

        // boxManager.CreateBox(transform, 10, new float[] { 1.0f, 0.5f, 0.0f }); // Example box
        // boxManager.CreateBox(transform, 15, new float[] { 0.0f, 1.0f, 0.0f }); // Another example box

        // // Assuming at some point you want to remove all red boxes
        // boxManager.RemoveBoxesByColor(new float[] { 1.0f, 0.0f, 0.0f }); // This would remove boxes with exactly this RGB value

        StartListeningForMessages();
    }

    private void OnDestroy()
    {
        isServerRunning = false;
        udpListener1?.Close();
        udpListener2?.Close();
    }

    private void StartListeningForMessages()
    {
        Task.Run(async () => await ProcessMessageAsync(udpListener1, 1));
        Task.Run(async () => await ProcessMessageAsync(udpListener2, 2));
    }

    private async Task ProcessMessageAsync(UdpClient udpClient, int sourceId)
    {
        while (isServerRunning)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0/* 12344 + sourceId */);
            UdpReceiveResult receiveResult = await udpClient.ReceiveAsync();
            // byte[] bytesReceived = receiveResult.Buffer;
            string message = Encoding.ASCII.GetString(receiveResult.Buffer);
            Debug.Log($"Received message from source {sourceId}: {message}");
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                /* if (sourceId == 1)
                    latestMessage1 = message;
                else
                    latestMessage2 = message; */
                switch(sourceId)
                {
                    case 1: latestMessage1 = message; break;
                    case 2: latestMessage2 = message; break;
                    default: break;
                }
            });
        }
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        totalElapsedTime += Time.deltaTime;
        if (!string.IsNullOrEmpty(latestMessage1) && !string.IsNullOrEmpty(latestMessage2))
        {
            //depending on the message, spawn a box, move a current box
        }
        if (timeElapsed >= 1.0f)
        {
            Debug.Log($"Amount of movements in last second: {counter}");
            Debug.Log($"Average movement per second: {(float)totalCounter / totalElapsedTime}");
            timeElapsed = 0.0f;
            counter = 0;
        }
    }
}