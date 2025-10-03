using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Service_contract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace ServiceLayer.Services.Auth.Notification
{
    public class FCMService : IFCMService
    {
        private readonly ILogger<FCMService> _logger;
        private readonly FirebaseMessaging _messaging;

        public FCMService(ILogger<FCMService> logger, IConfiguration configuration)
        {
            _logger = logger;

            try
            {
                var projectId = configuration["FCM:ProjectId"];

                if (FirebaseApp.DefaultInstance == null)
                {
                    GoogleCredential credential = null;

                    // Priority 1: Check for base64 encoded credentials in environment variable
                    var credentialsBase64 = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_BASE64");
                    if (!string.IsNullOrEmpty(credentialsBase64))
                    {
                        _logger.LogInformation("Initializing Firebase with base64 encoded credentials from environment variable");
                        try
                        {
                            var credentialsJson = Encoding.UTF8.GetString(Convert.FromBase64String(credentialsBase64));
                            credential = GoogleCredential.FromJson(credentialsJson);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to decode base64 credentials");
                        }
                    }

                    // Priority 2: Check for JSON credentials in environment variable
                    if (credential == null)
                    {
                        var credentialsJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON");
                        if (!string.IsNullOrEmpty(credentialsJson))
                        {
                            _logger.LogInformation("Initializing Firebase with JSON credentials from environment variable");
                            credential = GoogleCredential.FromJson(credentialsJson);
                        }
                    }

                    // Priority 3: Check for credentials file path
                    if (credential == null)
                    {
                        var credentialsPath = configuration["FCM:CredentialsPath"];
                        if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
                        {
                            _logger.LogInformation("Initializing Firebase with credentials file at {Path}", credentialsPath);
                            credential = GoogleCredential.FromFile(credentialsPath);
                        }
                    }

                    if (credential == null)
                    {
                        _logger.LogWarning("No Firebase credentials found. FCM notifications will not be available.");
                        return;
                    }

                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = credential,
                        ProjectId = projectId
                    });

                    _messaging = FirebaseMessaging.DefaultInstance;
                    _logger.LogInformation("Firebase initialized successfully");
                }
                else
                {
                    _messaging = FirebaseMessaging.DefaultInstance;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase. FCM notifications will not be available.");
                // Don't throw - allow app to continue without FCM
            }
        }

        public async Task<bool> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string> data = null)
        {
            if (_messaging == null)
            {
                _logger.LogWarning("Firebase messaging is not initialized. Cannot send notification.");
                return false;
            }

            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Device token is null or empty");
                    return false;
                }

                var message = new Message()
                {
                    Token = deviceToken,
                    Notification = new FirebaseAdmin.Messaging.Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data ?? new Dictionary<string, string>(),
                    Android = new AndroidConfig()
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification()
                        {
                            Title = title,
                            Body = body,
                            ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                        }
                    },
                    Apns = new ApnsConfig()
                    {
                        Aps = new Aps()
                        {
                            Alert = new ApsAlert()
                            {
                                Title = title,
                                Body = body
                            },
                            Badge = 1,
                            Sound = "default"
                        }
                    }
                };

                var response = await _messaging.SendAsync(message);
                _logger.LogInformation($"Successfully sent message: {response}");
                return true;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, $"Firebase messaging error: {ex.MessagingErrorCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending FCM notification");
                return false;
            }
        }

        public async Task<bool> SendNotificationToMultipleAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string> data = null)
        {
            if (_messaging == null)
            {
                _logger.LogWarning("Firebase messaging is not initialized. Cannot send notifications.");
                return false;
            }

            try
            {
                if (deviceTokens == null || !deviceTokens.Any())
                {
                    _logger.LogWarning("No device tokens provided");
                    return false;
                }

                var message = new MulticastMessage()
                {
                    Tokens = deviceTokens,
                    Notification = new FirebaseAdmin.Messaging.Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data ?? new Dictionary<string, string>(),
                    Android = new AndroidConfig()
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification()
                        {
                            Title = title,
                            Body = body,
                            ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                        }
                    },
                    Apns = new ApnsConfig()
                    {
                        Aps = new Aps()
                        {
                            Alert = new ApsAlert()
                            {
                                Title = title,
                                Body = body
                            },
                            Badge = 1,
                            Sound = "default"
                        }
                    }
                };

                var response = await _messaging.SendEachForMulticastAsync(message);
                _logger.LogInformation($"Successfully sent {response.SuccessCount} messages out of {deviceTokens.Count}");

                if (response.FailureCount > 0)
                {
                    _logger.LogWarning($"Failed to send {response.FailureCount} messages");
                    foreach (var error in response.Responses.Where(r => !r.IsSuccess))
                    {
                        _logger.LogError($"FCM Error: {error.Exception?.Message}");
                    }
                }

                return response.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending FCM notifications to multiple devices");
                return false;
            }
        }
    }
}