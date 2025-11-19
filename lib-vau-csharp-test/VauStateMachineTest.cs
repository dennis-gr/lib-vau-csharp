/*
 * Copyright 2024 gematik GmbH
 *
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
 *
 * For additional notes and disclaimer from gematik and in case of changes by gematik find details in the "Readme" file.
 */

using lib_vau_csharp;
using lib_vau_csharp.data;

using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace lib_vau_csharp_test
{
    public class VauStateMachineTest
    {
        private SignedPublicVauKeys signedPublicVauKeys;
        
        private VauClientStateMachine client;
        private VauServerStateMachine server;

        [SetUp]
        public void Setup()
        {
            VauPublicKeys vauBasicPublicKey = new VauPublicKeys(Constants.Keys.EccKyberKeyPair, "VAU Server Keys", TimeSpan.FromDays(30));
            signedPublicVauKeys = SignedPublicVauKeys.Sign(Constants.Certificates.ServerAutCertificate, Constants.Keys.ECPrivateKeyParameters, Constants.Certificates.OcspResponseAutCertificate, 1, vauBasicPublicKey);
            
            client = new VauClientStateMachine();
            server = new VauServerStateMachine(signedPublicVauKeys, Constants.Keys.EccKyberKeyPair);

            byte[] pMessage1 = client.generateMessage1();
            byte[] pMessage2 = server.receiveMessage1(pMessage1);
            byte[] pMessage3 = client.receiveMessage2(pMessage2);
            byte[] pMessage4 = server.receiveMessage3(pMessage3);
            client.receiveMessage4(pMessage4);
        }

        [Test]
        public void SimpleTest()
        {
            Assert.DoesNotThrow(() =>
            {
                int messageCount = 3;
                long before = client.RequestCounter;
                for (int i = 0; i < messageCount; i++)
                {
                    // Client encrypts request
                    byte[] request = System.Text.Encoding.UTF8.GetBytes($"Hello {i}");
                    byte[] encryptedRequest = client.EncryptVauMessage(request);

                    // Server decrypts request
                    byte[] decryptedRequest = server.DecryptVauMessage(encryptedRequest);
                    Assert.That(request.SequenceEqual(decryptedRequest), $"Decrypted request {i} should match original");

                    // Server encrypts response
                    byte[] response = System.Text.Encoding.UTF8.GetBytes($"Response {i}");
                    byte[] encryptedResponse = server.EncryptVauMessage(response);

                    // Client decrypts response
                    byte[] decryptedResponse = client.DecryptVauMessage(encryptedResponse);
                    Assert.That(response.SequenceEqual(decryptedResponse), $"Decrypted response {i} should match original");
                }
                long after = client.RequestCounter;
                Assert.That((before + messageCount == after), $"Request counter should increment by {messageCount} after sending {messageCount} messages, but got {after}");
            });
        }

        [Test]
        public async Task Incrementing_The_RequestCounter_Is_Thread_Safe()
        {
            const int requestCount = 75;
            
            var tasks = new Task[requestCount];
            
            for (int i = 0; i < requestCount; i++)
            {
                tasks[i] = Task.Run(() => client.EncryptVauMessage(Array.Empty<byte>()));
            }
            
            await Task.WhenAll(tasks);
            
            Assert.That(client.RequestCounter, Is.EqualTo(requestCount));
        }
    }
}
