﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace resque
{
    public class NoQueueError : Exception
    {
    }
    public class Resque
    {
        private static Redis staticRedis;
        public static void setRedis(Redis redis)
        {
            staticRedis = redis;
        }
        public static Redis redis()
        {
            if (staticRedis == null)
            {
                staticRedis = new Redis();
            }
            return staticRedis;
        }

        public static bool Push(string queue, object item)
        {
            watchQueue(queue);
            redis().RightPush("queue:" + queue, encode(item));
            return true;
        }

        public static Dictionary<string, object> Pop(string queue)
        {
            var data = redis().LeftPop("queue:" + queue);
            return decodeData(data);
        }



        public static Dictionary<string, object> Peek(string queue)
        {
            var data = redis().ListIndex("queue:" + queue, 0);
            return decodeData(data);
        }

        public static Dictionary<string, object> Peek(string queue, int start)
        {
            var resultData = redis().ListRange("queue:" + queue, start, start);
            if (resultData.Length == 0)
            {
                return null;
            } else {
              return decodeData(resultData[0]);
             }
        }

        public static ArrayList Peek(string queue, int start, int count)
        {
            ArrayList results = new ArrayList();
            if (count == 1)
            {
                results.Add(Peek(queue, start));
            }
            else
            {
                foreach (byte[] data in redis().ListRange("queue:" + queue, start, start + count - 1))
                {
                    results.Add(decodeData(data));
                }
            }
            return results;
        }

        private static void watchQueue(string queue)
        {
            redis().AddToSet("queues", queue);
        }

        public static string[] queues()
        {
            byte[][] rawResults = redis().GetMembersOfSet("queues");
            if (rawResults.Length == 0)
                return new string[0];
            string[] results = new string[rawResults.Length];
            int i = 0;
            foreach (byte[] data in rawResults)
            {
                results[i] = Encoding.UTF8.GetString(data);
                i++;
            }
            return results;
        }



        public static Job Reserve(string queue)
        {
            return Job.Reserve(queue);
        }



        public static int size(string queue)
        {
            return redis().ListLength("queue:" + queue);
        }

        public static bool enqueue(string className, params object[] args)
        {
            Type workerType = Type.GetType(className);
            System.Reflection.MethodInfo methodInfo = workerType.GetMethod("queue", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy);
            if(methodInfo == null)
                throw new NoQueueError();
            string queue = (string)methodInfo.Invoke(null, null);
            if (String.IsNullOrEmpty(queue))
                throw new NoQueueError();
            return Job.create(queue, className, args);
        }


        #region encoding
        private static string encode(object item)
        {
            return JsonConvert.SerializeObject(item);
        }

        private static object decode(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }
        private static object decode(byte[] json)
        {
            return decode(Encoding.UTF8.GetString(json));
        }

        private static Dictionary<string, object> decodeData(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            else
            {
                return (Dictionary<string, object>)decode(data);
            }
        }
        #endregion

    }
}
