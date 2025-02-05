﻿////////////////////////////////////////////////////////////////////////////
//
// Copyright 2016 Realm Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Realms.Exceptions;

namespace Realms.Tests.Database
{
    [TestFixture, Preserve(AllMembers = true)]
    public class InMemoryTests : RealmTest
    {
        private InMemoryConfiguration _config;

        protected override void CustomSetUp()
        {
            base.CustomSetUp();

            _config = new InMemoryConfiguration(Guid.NewGuid().ToString());
        }

        [Test]
        public void InMemoryRealm_WhenDeleted_RemovesAuxiliaryFiles()
        {
            using (var realm = GetRealm(_config))
            {
                realm.Write(() => realm.Add(new IntPropertyObject
                {
                    Int = 42
                }));

                Assert.That(File.Exists(_config.DatabasePath));
                Assert.That(realm.All<IntPropertyObject>().Single().Int, Is.EqualTo(42));
            }

            Assert.That(File.Exists(_config.DatabasePath), Is.False);

            using (var realm = GetRealm(_config))
            {
                Assert.That(File.Exists(_config.DatabasePath));
                Assert.That(realm.All<IntPropertyObject>(), Is.Empty);
            }
        }

        [Test]
        public void InMemoryRealm_ReceivesNotifications()
        {
            TestHelpers.RunAsyncTest(async () =>
            {
                var tcs = new TaskCompletionSource<ChangeSet>();

                using var realm = GetRealm(_config);

                var query = realm.All<IntPropertyObject>();
                using var token = query.SubscribeForNotifications((sender, changes, error) =>
                {
                    if (changes != null)
                    {
                        tcs.TrySetResult(changes);
                    }
                    else if (error != null)
                    {
                        tcs.TrySetException(error);
                    }
                });

                await Task.Delay(100);

                await Task.Run(() =>
                {
                    using var otherRealm = GetRealm(_config);
                    otherRealm.Write(() => otherRealm.Add(new IntPropertyObject
                    {
                        Int = 42
                    }));
                });

                var backgroundChanges = await tcs.Task;

                Assert.That(backgroundChanges.InsertedIndices, Is.Not.Empty);
                Assert.That(backgroundChanges.DeletedIndices, Is.Empty);
                Assert.That(backgroundChanges.ModifiedIndices, Is.Empty);
                Assert.That(backgroundChanges.InsertedIndices[0], Is.EqualTo(0));
            });
        }

        [Test]
        public void InMemoryRealm_WhenMultipleInstancesOpen_DoesntDeleteData()
        {
            var first = GetRealm(_config);
            var second = GetRealm(_config);

            first.Write(() => first.Add(new IntPropertyObject
            {
                Int = 42
            }));

            Assert.That(File.Exists(_config.DatabasePath));
            Assert.That(second.All<IntPropertyObject>().Single().Int, Is.EqualTo(42));

            first.Dispose();

            Assert.That(File.Exists(_config.DatabasePath));
            Assert.That(second.All<IntPropertyObject>().Single().Int, Is.EqualTo(42));

            second.Dispose();

            Assert.That(File.Exists(_config.DatabasePath), Is.False);
        }

        [Test]
        public void InMemoryRealm_WhenGarbageCollected_DeletesData()
        {
            TestHelpers.RunAsyncTest(async () =>
            {
                await TestHelpers.EnsureObjectsAreCollected(() =>
                {
                    var realm = Realm.GetInstance(_config);
                    realm.Write(() => realm.Add(new IntPropertyObject
                    {
                        Int = 42
                    }));

                    return new[] { realm };
                });

                // Sometimes it takes a little while for the file to be deleted
                await Task.Delay(200);

                Assert.That(File.Exists(_config.DatabasePath), Is.False);

                using var realm2 = GetRealm(_config);
                Assert.That(realm2.All<IntPropertyObject>(), Is.Empty);
            });
        }

        [Test]
        [Obsolete("Tests obsolete functionality")]
        public void InMemoryRealm_WhenEncrypted_Throws()
        {
            Assert.Throws<NotSupportedException>(() => _ = new InMemoryConfiguration(_config.Identifier)
            {
                EncryptionKey = TestHelpers.GetEncryptionKey(23)
            });
        }

        [Test]
        public void InMemoryRealmWithFrozenObjects_WhenDeleted_DoesNotThrow()
        {
            var realm = GetRealm(_config);
            var frozenObj = realm.Write(() =>
            {
                return realm.Add(new IntPropertyObject
                {
                    Int = 1
                }).Freeze();
            });

            frozenObj.Realm.Dispose();
            realm.Dispose();

            Assert.That(frozenObj.Realm.IsClosed, Is.True);
            Assert.That(realm.IsClosed, Is.True);
            Assert.DoesNotThrow(() => Realm.DeleteRealm(_config));
        }
    }
}
