﻿#if !UNITY_EDITOR
using System;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace HeadVoiceSelector.Utils
{
    internal abstract class WebRequestUtils
    {
        public static T Get<T>(string url)
        {
            var req = RequestHandler.GetJson(url);
            return JsonConvert.DeserializeObject<T>(req);
        }
        public static T Post<T>(string url, string data)
        {
            if (url == null)
            {
                Console.WriteLine("Error: url is null.");
                return default(T);
            }

            if (data == null)
            {
                Console.WriteLine("Error: data is null.");
                return default(T);
            }

            try
            {
                // Convert the string data to JSON format
                string jsonData = JsonConvert.SerializeObject(new { Data = data });
#if DEBUG
                Console.WriteLine($"Sending JSON data to {url}: {jsonData}");
#endif
                var req = RequestHandler.PostJson(url, jsonData);
#if DEBUG
                Console.WriteLine($"Received response: {req}");
#endif
                if (req == null)
                {
                    Console.WriteLine("Error: Response is null.");
                    return default(T);
                }

                return default(T);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred during request: {ex.Message}");
                return default(T);
            }
        }




    }

}

#endif