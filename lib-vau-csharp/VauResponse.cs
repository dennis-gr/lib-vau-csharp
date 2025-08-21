/*
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Linq;
using System.Net;
using System.Net.Http;

using lib_vau_csharp.util;

namespace lib_vau_csharp
{
    public class VauResponse
    {
        private const int StatusCodeIndex = 1;
        private const int ReasonPhraseIndex = 2;

        private static readonly char[] HeaderSplit = [':'];

        /// <summary>
        /// Parses the given <paramref name="decryptedResponse"/> into the given <paramref name="httpResponseMessage"/>.
        /// </summary>
        /// <param name="decryptedResponse"></param>
        /// <param name="httpResponseMessage"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Parse(string decryptedResponse, HttpResponseMessage httpResponseMessage)
        {
            if (decryptedResponse == null)
                throw new ArgumentNullException(nameof(decryptedResponse));
            if (httpResponseMessage == null)
                throw new ArgumentNullException(nameof(httpResponseMessage));

            var lines = decryptedResponse.ReadLines().ToList();

            int contentIndex = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (i == 0)
                {
                    if (line.StartsWith("HTTP"))
                    {
                        string[] status = lines[0].Split(' ');
                        if (Enum.TryParse(status[StatusCodeIndex], true, out HttpStatusCode statusCode))
                            httpResponseMessage.StatusCode = statusCode;

                        httpResponseMessage.ReasonPhrase = status[ReasonPhraseIndex];   
                    }
                    else
                    {
                        //Not sure if we can get here in a production env, the TestSendingMessagesThroughChannel test expects this work though 
                        break;
                    }
                }

                if (String.IsNullOrWhiteSpace(line))
                {
                    contentIndex = i + 1;
                    break;
                }

                string[] headerNameValue = line.Split(HeaderSplit, 2);
                string name = headerNameValue[0].Trim();

                if (HttpResponseHeaderNames.All.Contains(name))
                {
                    string value = headerNameValue[1].Trim();
                    httpResponseMessage.Headers.Add(name, value);
                }
            }

            httpResponseMessage.Content = contentIndex < lines.Count ? new StringContent(String.Join(String.Empty, lines.Skip(contentIndex))) : null;
        }
    }
}