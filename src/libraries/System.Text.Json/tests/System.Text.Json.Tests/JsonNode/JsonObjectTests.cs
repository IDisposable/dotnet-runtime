﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Nodes.Tests
{
    public static class JsonObjectTests
    {
        [Fact]
        public static void KeyValuePair()
        {
            var jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["Two"] = 2;
            Test();

            jObject = new JsonObject { { "One", 1 }, { "Two", 2 } };
            Test();

            void Test()
            {
                KeyValuePair<string, JsonNode?> kvp1 = default;
                KeyValuePair<string, JsonNode?> kvp2 = default;

                int count = 0;
                foreach (KeyValuePair<string, JsonNode?> kvp in jObject)
                {
                    if (count == 0)
                    {
                        kvp1 = kvp;
                    }
                    else
                    {
                        kvp2 = kvp;
                    }

                    count++;
                }

                Assert.Equal(2, count);

                ICollection<KeyValuePair<string, JsonNode?>> iCollection = jObject;
                Assert.True(iCollection.Contains(kvp1));
                Assert.True(iCollection.Contains(kvp2));
                Assert.False(iCollection.Contains(new KeyValuePair<string, JsonNode?>("?", null)));

                Assert.True(iCollection.Remove(kvp1));
                Assert.Equal(1, jObject.Count);

                Assert.False(iCollection.Remove(new KeyValuePair<string, JsonNode?>("?", null)));
                Assert.Equal(1, jObject.Count);
            }
        }

        [Fact]
        public static void IsReadOnly()
        {
            var jsonObject = new JsonObject();

            ICollection<KeyValuePair<string, JsonNode?>> iCollectionOfKVP = jsonObject;
            Assert.False(iCollectionOfKVP.IsReadOnly);

            IDictionary<string, JsonNode?> iDictionary = jsonObject;
            Assert.True(iDictionary.Keys.IsReadOnly);
            Assert.True(iDictionary.Values.IsReadOnly);
        }

        [Fact]
        public static void KeyAndValueCollections_ThrowsNotSupportedException()
        {
            IDictionary<string, JsonNode?> jsonObject = new JsonObject();

            Assert.Throws<NotSupportedException>(() => jsonObject.Keys.Add("Hello"));
            Assert.Throws<NotSupportedException>(() => jsonObject.Values.Add("Hello"));

            Assert.Throws<NotSupportedException>(() => jsonObject.Keys.Clear());
            Assert.Throws<NotSupportedException>(() => jsonObject.Values.Clear());

            Assert.Throws<NotSupportedException>(() => jsonObject.Keys.Remove("Hello"));
            Assert.Throws<NotSupportedException>(() => jsonObject.Values.Remove("Hello"));
        }

        [Fact]
        public static void NullPropertyValues()
        {
            var jObject = new JsonObject();
            jObject["One"] = null;
            jObject.Add("Two", null);
            Assert.Equal(2, jObject.Count);
            Assert.Null(jObject["One"]);
            Assert.Null(jObject["Two"]);
        }

        [Fact]
        public static void NullPropertyNameFail()
        {
            ArgumentNullException ex;

            var jObject = new JsonObject();
            ex = Assert.Throws<ArgumentNullException>(() => jObject.Add(null, 42));
            Assert.Contains("propertyName", ex.Message);

            ex = Assert.Throws<ArgumentNullException>(() => jObject[null] = 42);
            Assert.Contains("propertyName", ex.Message);

            ex = Assert.Throws<ArgumentNullException>(() => jObject.ContainsKey(null));
            Assert.Contains("propertyName", ex.Message);

            ex = Assert.Throws<ArgumentNullException>(() => jObject.Remove(null));
            Assert.Contains("propertyName", ex.Message);

            var iDictionary = (IDictionary<string, JsonNode?>)jObject;
            ex = Assert.Throws<ArgumentNullException>(() => iDictionary.TryGetValue(null, out JsonNode _));
            Assert.Contains("propertyName", ex.Message);
        }

        [Fact]
        public static void IEnumerable()
        {
            var jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["Two"] = 2;

            IEnumerable enumerable = jObject;
            int count = 0;
            foreach (KeyValuePair<string, JsonNode?> kvp in enumerable)
            {
                count++;
            }

            Assert.Equal(2, count);
        }

        [Fact]
        public static void MissingProperty()
        {
            var options = new JsonSerializerOptions();
            JsonObject jObject = JsonSerializer.Deserialize<JsonObject>("{}", options);
            Assert.Null(jObject["NonExistingProperty"]);
            Assert.False(jObject.Remove("NonExistingProperty"));
        }

        [Fact]
        public static void IDictionary_KeyValuePair()
        {
            IDictionary<string, JsonNode?> jObject = new JsonObject();
            jObject.Add(new KeyValuePair<string, JsonNode?>("MyProperty", 42));
            Assert.Equal(42, jObject["MyProperty"].GetValue<int>());

            Assert.Equal(1, jObject.Keys.Count);
            Assert.Equal(1, jObject.Values.Count);
        }

        [Fact]
        public static void Clear_ContainsKey()
        {
            var jObject = new JsonObject();
            jObject.Add("One", 1);
            jObject.Add("Two", 2);
            Assert.Equal(2, jObject.Count);

            Assert.True(jObject.ContainsKey("One"));
            Assert.True(jObject.ContainsKey("Two"));
            Assert.False(jObject.ContainsKey("?"));

            jObject.Clear();
            Assert.False(jObject.ContainsKey("One"));
            Assert.False(jObject.ContainsKey("Two"));
            Assert.False(jObject.ContainsKey("?"));
            Assert.Equal(0, jObject.Count);

            jObject.Add("One", 1);
            jObject.Add("Two", 2);
            Assert.Equal(2, jObject.Count);
        }

        [Fact]
        public static void Clear_JsonElement()
        {
            const string Json = "{\"MyProperty\":42}";
            JsonObject obj;

            // Baseline
            obj = JsonSerializer.Deserialize<JsonObject>(Json);
            Assert.Equal(1, obj.Count);
            obj.Clear();
            Assert.Equal(0, obj.Count);

            // Call clear with only JsonElement (not yet expanded to JsonNodes).
            obj = JsonSerializer.Deserialize<JsonObject>(Json);
            obj.Clear();
            // Don't check for Count of 1 since that will create nodes.
            Assert.Equal(0, obj.Count);
        }

        [Fact]
        public static void CaseSensitivity_ReadMode()
        {
            var options = new JsonSerializerOptions();
            JsonObject obj = JsonSerializer.Deserialize<JsonObject>("{\"MyProperty\":42}", options);

            Assert.Equal(42, obj["MyProperty"].GetValue<int>());
            Assert.Null(obj["myproperty"]);
            Assert.Null(obj["MYPROPERTY"]);

            options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;
            obj = JsonSerializer.Deserialize<JsonObject>("{\"MyProperty\":42}", options);

            Assert.Equal(42, obj["MyProperty"].GetValue<int>());
            Assert.Equal(42, obj["myproperty"].GetValue<int>());
            Assert.Equal(42, obj["MYPROPERTY"].GetValue<int>());
        }

        [Fact]
        public static void CaseInsensitive_Remove()
        {
            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            JsonObject obj = JsonSerializer.Deserialize<JsonObject>("{\"MyProperty\":42}", options);

            Assert.True(obj.ContainsKey("MyProperty"));
            Assert.True(obj.ContainsKey("myproperty"));
            Assert.True(obj.ContainsKey("MYPROPERTY"));

            Assert.True(obj.Remove("myproperty"));
            Assert.False(obj.Remove("myproperty"));
            Assert.False(obj.Remove("MYPROPERTY"));
            Assert.False(obj.Remove("MyProperty"));
        }

        [Fact]
        public static void CaseSensitive_Remove()
        {
            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = false };
            JsonObject obj = JsonSerializer.Deserialize<JsonObject>("{\"MYPROPERTY\":42,\"myproperty\":43}", options);

            Assert.False(obj.ContainsKey("MyProperty"));
            Assert.True(obj.ContainsKey("MYPROPERTY"));
            Assert.True(obj.ContainsKey("myproperty"));

            Assert.False(obj.Remove("MyProperty"));

            Assert.True(obj.Remove("MYPROPERTY"));
            Assert.False(obj.Remove("MYPROPERTY"));

            Assert.True(obj.Remove("myproperty"));
            Assert.False(obj.Remove("myproperty"));
        }

        [Fact]
        public static void CaseSensitivity_EditMode()
        {
            var jArray = new JsonArray();
            var jObject = new JsonObject();
            jObject.Add("MyProperty", 42);
            jObject.Add("myproperty", 42); // No exception

            // Options on direct node.
            var options = new JsonNodeOptions { PropertyNameCaseInsensitive = true };
            jArray = new JsonArray();
            jObject = new JsonObject(options);
            jObject.Add("MyProperty", 42);
            jArray.Add(jObject);
            Assert.Throws<ArgumentException>(() => jObject.Add("myproperty", 42));

            // Options on parent node (deferred creation of options until Add).
            jArray = new JsonArray(options);
            jObject = new JsonObject();
            jArray.Add(jObject);
            jObject.Add("MyProperty", 42);
            Assert.Throws<ArgumentException>(() => jObject.Add("myproperty", 42));

            // Dictionary is created when Add is called for the first time, so we need to be added first.
            jArray = new JsonArray(options);
            jObject = new JsonObject();
            jObject.Add("MyProperty", 42);
            jArray.Add(jObject);
            jObject.Add("myproperty", 42); // No exception since options were not set in time.
        }

        [Fact]
        public static void NamingPoliciesAreNotUsed()
        {
            const string Json = "{\"myProperty\":42}";

            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;

            JsonObject obj = JsonSerializer.Deserialize<JsonObject>(Json, options);
            string json = obj.ToJsonString();
            JsonTestHelper.AssertJsonEqual(Json, json);
        }

        [Fact]
        public static void FromElement()
        {
            using (JsonDocument document = JsonDocument.Parse("{\"myProperty\":42}"))
            {
                JsonObject jObject = JsonObject.Create(document.RootElement);
                Assert.Equal(42, jObject["myProperty"].GetValue<int>());
            }

            using (JsonDocument document = JsonDocument.Parse("null"))
            {
                JsonObject? jObject = JsonObject.Create(document.RootElement);
                Assert.Null(jObject);
            }
        }

        [Theory]
        [InlineData("42")]
        [InlineData("[]")]
        public static void FromElement_WrongNodeTypeThrows(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                Assert.Throws<InvalidOperationException>(() => JsonObject.Create(document.RootElement));
            }
        }

        [Fact]
        public static void WriteTo_Validation()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonObject().WriteTo(null));
        }

        [Fact]
        public static void WriteTo()
        {
            const string Json = "{\"MyProperty\":42}";

            JsonObject jObject = JsonNode.Parse(Json).AsObject();
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            jObject.WriteTo(writer);
            writer.Flush();

            string json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(Json, json);
        }

        [Fact]
        public static void CopyTo()
        {
            JsonNode node1 = 1;
            JsonNode node2 = 2;

            IDictionary<string, JsonNode?> jObject = new JsonObject();
            jObject["One"] = node1;
            jObject["Two"] = node2;
            jObject["null"] = null;

            var arr = new KeyValuePair<string, JsonNode?>[4];
            jObject.CopyTo(arr, 0);

            Assert.Same(node1, arr[0].Value);
            Assert.Same(node2, arr[1].Value);
            Assert.Null(arr[2].Value);

            arr = new KeyValuePair<string, JsonNode?>[5];
            jObject.CopyTo(arr, 1);
            Assert.Null(arr[0].Key);
            Assert.Null(arr[0].Value);
            Assert.NotNull(arr[1].Key);
            Assert.Same(node1, arr[1].Value);
            Assert.NotNull(arr[2].Key);
            Assert.Same(node2, arr[2].Value);
            Assert.NotNull(arr[3].Key);
            Assert.Null(arr[3].Value);
            Assert.Null(arr[4].Key);
            Assert.Null(arr[4].Value);

            arr = new KeyValuePair<string, JsonNode?>[3];
            Assert.Throws<ArgumentException>(() => jObject.CopyTo(arr, 1));

            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.CopyTo(arr, -1));
        }

        [Fact]
        public static void CopyTo_KeyAndValueCollections()
        {
            IDictionary<string, JsonNode?> jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["Two"] = 2;

            string[] stringArray = new string[3];
            jObject.Keys.CopyTo(stringArray, 1);
            Assert.Null(stringArray[0]);
            Assert.Equal("One", stringArray[1]);
            Assert.Equal("Two", stringArray[2]);

            JsonNode[] nodeArray = new JsonNode[3];
            jObject.Values.CopyTo(nodeArray, 1);
            Assert.Null(nodeArray[0]);
            Assert.Equal(1, nodeArray[1].GetValue<int>());
            Assert.Equal(2, nodeArray[2].GetValue<int>());

            Assert.Throws<ArgumentException>(() => jObject.Keys.CopyTo(stringArray, 2));
            Assert.Throws<ArgumentException>(() => jObject.Values.CopyTo(nodeArray, 2));

            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.Keys.CopyTo(stringArray, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.Values.CopyTo(nodeArray, -1));
        }

        [Fact]
        public static void Contains_KeyAndValueCollections()
        {
            IDictionary<string, JsonNode?> jObject = new JsonObject();
            jObject["One"] = 1;

            Assert.True(jObject.Keys.Contains("One"));
            Assert.False(jObject.Keys.Contains("Two"));

            Assert.True(jObject.Values.Contains(jObject["One"]));
            Assert.False(jObject.Values.Contains(1)); // Reference semantics causes this to be false.
        }

        [Fact]
        public static void IEnumerable_KeyAndValueCollections()
        {
            IDictionary<string, JsonNode?> jObject = new JsonObject();
            jObject["One"] = 1;

            IEnumerable enumerable = jObject.Keys;
            int count = 0;
            foreach (string str in enumerable)
            {
                count++;
            }
            Assert.Equal(1, count);

            enumerable = jObject.Values;
            count = 0;
            foreach (JsonNode node in enumerable)
            {
                count++;
            }
            Assert.Equal(1, count);
        }

        [Fact]
        public static void CreateDom()
        {
            var jObj = new JsonObject
            {
                // Primitives
                ["MyString"] = JsonValue.Create("Hello!"),
                ["MyNull"] = null,
                ["MyBoolean"] = JsonValue.Create(false),

                // Nested array
                ["MyArray"] = new JsonArray
                (
                    JsonValue.Create(2),
                    JsonValue.Create(3),
                    JsonValue.Create(42)
                ),

                // Additional primitives
                ["MyInt"] = JsonValue.Create(43),
                ["MyDateTime"] = JsonValue.Create(new DateTime(2020, 7, 8)),
                ["MyGuid"] = JsonValue.Create(new Guid("ed957609-cdfe-412f-88c1-02daca1b4f51")),

                // Nested objects
                ["MyObject"] = new JsonObject
                {
                    ["MyString"] = JsonValue.Create("Hello!!")
                },

                ["Child"] = new JsonObject
                {
                    ["ChildProp"] = JsonValue.Create(1)
                }
            };

            string json = jObj.ToJsonString();
            JsonTestHelper.AssertJsonEqual(JsonNodeTests.ExpectedDomJson, json);
        }

        [Fact]
        public static void CreateDom_ImplicitOperators()
        {
            var jObj = new JsonObject
            {
                // Primitives
                ["MyString"] = "Hello!",
                ["MyNull"] = null,
                ["MyBoolean"] = false,

                // Nested array
                ["MyArray"] = new JsonArray(2, 3, 42),

                // Additional primitives
                ["MyInt"] = 43,
                ["MyDateTime"] = new DateTime(2020, 7, 8),
                ["MyGuid"] = new Guid("ed957609-cdfe-412f-88c1-02daca1b4f51"),

                // Nested objects
                ["MyObject"] = new JsonObject
                {
                    ["MyString"] = "Hello!!"
                },

                ["Child"] = new JsonObject()
                {
                    ["ChildProp"] = 1
                }
            };

            string json = jObj.ToJsonString();
            JsonTestHelper.AssertJsonEqual(JsonNodeTests.ExpectedDomJson, json);
        }

        [Fact]
        public static void EditDom()
        {
            const string Json =
                "{\"MyString\":\"Hello\",\"MyNull\":null,\"MyBoolean\":true,\"MyArray\":[1,2],\"MyInt\":42,\"MyDateTime\":\"2020-07-08T00:00:00\",\"MyGuid\":\"ed957609-cdfe-412f-88c1-02daca1b4f51\",\"MyObject\":{\"MyString\":\"World\"}}";

            JsonNode obj = JsonSerializer.Deserialize<JsonObject>(Json);
            Verify();

            // Verify the values are round-trippable.
            ((JsonArray)obj["MyArray"]).RemoveAt(2);
            Verify();

            void Verify()
            {
                // Change some primitives.
                obj["MyString"] = JsonValue.Create("Hello!");
                obj["MyBoolean"] = JsonValue.Create(false);
                obj["MyInt"] = JsonValue.Create(43);

                // Add nested objects.
                obj["MyObject"] = new JsonObject();
                obj["MyObject"]["MyString"] = JsonValue.Create("Hello!!");

                obj["Child"] = new JsonObject();
                obj["Child"]["ChildProp"] = JsonValue.Create(1);

                // Modify number elements.
                obj["MyArray"][0] = JsonValue.Create(2);
                obj["MyArray"][1] = JsonValue.Create(3);

                // Add an element.
                ((JsonArray)obj["MyArray"]).Add(JsonValue.Create(42));

                string json = obj.ToJsonString();
                JsonTestHelper.AssertJsonEqual(JsonNodeTests.ExpectedDomJson, json);
            }
        }

        [Fact]
        public static void ReAddSameNode_Throws()
        {
            var jValue = JsonValue.Create(1);

            var jObject = new JsonObject();
            jObject.Add("Prop", jValue);
            ArgumentException ex = Assert.Throws<ArgumentException>(() => jObject.Add("Prop", jValue));
            Assert.Contains("Prop", ex.Message);
        }

        [Fact]
        public static void ReAddRemovedNode()
        {
            var jValue = JsonValue.Create(1);

            var jObject = new JsonObject();
            jObject.Add("Prop", jValue);
            Assert.Equal(jObject, jValue.Parent);

            // Replace with a new node.
            jObject["Prop"] = 42;

            Assert.Null(jValue.Parent);
            Assert.Equal(1, jObject.Count);
            jObject.Remove("Prop");
            Assert.Equal(0, jObject.Count);
            jObject.Add("Prop", jValue);
            Assert.Equal(1, jObject.Count);
        }

        [Fact]
        public static void DynamicObject_LINQ_Query()
        {
            JsonArray allOrders = JsonSerializer.Deserialize<JsonArray>(JsonNodeTests.Linq_Query_Json);
            IEnumerable<JsonNode> orders = allOrders.Where(o => o["Customer"]["City"].GetValue<string>() == "Fargo");

            Assert.Equal(2, orders.Count());
            Assert.Equal(100, orders.ElementAt(0)["OrderId"].GetValue<int>());
            Assert.Equal(300, orders.ElementAt(1)["OrderId"].GetValue<int>());
            Assert.Equal("Customer1", orders.ElementAt(0)["Customer"]["Name"].GetValue<string>());
            Assert.Equal("Customer3", orders.ElementAt(1)["Customer"]["Name"].GetValue<string>());
        }

        private class BlogPost
        {
            public string Title { get; set; }
            public string AuthorName { get; set; }
            public string AuthorTwitter { get; set; }
            public string Body { get; set; }
            public DateTime PostedDate { get; set; }
        }

        [Fact]
        public static void DynamicObject_LINQ_Convert()
        {
            string json = @"
            [
              {
                ""Title"": ""TITLE."",
                ""Author"":
                {
                  ""Name"": ""NAME."",
                  ""Mail"": ""MAIL."",
                  ""Picture"": ""/PICTURE.png""
                },
                ""Date"": ""2021-01-20T19:30:00"",
                ""BodyHtml"": ""Content.""
              }
            ]";

            JsonArray arr = JsonSerializer.Deserialize<JsonArray>(json);

            // Convert nested JSON to a flat POCO.
            IList<BlogPost> blogPosts = arr.Select(p => new BlogPost
            {
                Title = p["Title"].GetValue<string>(),
                AuthorName = p["Author"]["Name"].GetValue<string>(),
                AuthorTwitter = p["Author"]["Mail"].GetValue<string>(),
                PostedDate = p["Date"].GetValue<DateTime>(),
                Body = p["BodyHtml"].GetValue<string>()
            }).ToList();

            const string expected = "[{\"Title\":\"TITLE.\",\"AuthorName\":\"NAME.\",\"AuthorTwitter\":\"MAIL.\",\"Body\":\"Content.\",\"PostedDate\":\"2021-01-20T19:30:00\"}]";

            string json_out = JsonSerializer.Serialize(blogPosts);
            Assert.Equal(expected, json_out);
        }

        [Theory]
        [MemberData(nameof(JObjectCollectionData))]

        public static void ListToDictionaryConversions(JsonObject jObject, int count)
        {
            Assert.Equal(count, jObject.Count);

            int i;

            for (i = 0; i < count; i++)
            {
                Assert.Equal(i, jObject[i.ToString()].GetValue<int>());
            }

            i = 0;
            foreach (KeyValuePair<string, JsonNode?> kvp in jObject)
            {
                Assert.Equal(i.ToString(), kvp.Key);
                Assert.Equal(i, kvp.Value.GetValue<int>());
                i++;
            }

            i = 0;
            foreach (object o in (IEnumerable)jObject)
            {
                var kvp = (KeyValuePair<string, JsonNode?>)o;
                Assert.Equal(i.ToString(), kvp.Key);
                Assert.Equal(i, kvp.Value.GetValue<int>());
                i++;
            }

            var dictionary = (IDictionary<string, JsonNode?>)jObject;

            i = 0;
            foreach (string propertyName in dictionary.Keys)
            {
                Assert.Equal(i.ToString(), propertyName);
                i++;
            }

            i = 0;
            foreach (JsonNode? node in dictionary.Values)
            {
                Assert.Equal(i, node.GetValue<int>());
                i++;
            }

            string expectedJson = jObject.ToJsonString();

            // Use indexers to replace items.
            for (i = 0; i < count; i++)
            {
                string key = i.ToString();

                // Contains does a reference comparison on JsonNode so it needs to be done before modifying.
                Assert.True(jObject.Contains(new KeyValuePair<string, JsonNode?>(key, jObject[key])));

                jObject[key] = JsonValue.Create(i);
                jObject[key] = jObject[key]; // Should have no effect.

                Assert.False(jObject.Contains(new KeyValuePair<string, JsonNode?>("MISSING", jObject[key])));
                Assert.True(jObject.ContainsKey(key));

                // Remove() should not affect result when missing.
                bool success = jObject.Remove("MISSING");
                Assert.False(success);
            }

            // JSON shouldn't change.
            Assert.Equal(expectedJson, jObject.ToJsonString());

            // Remove every other entry.
            for (i = 0; i < count; i += 2)
            {
                bool success = dictionary.Remove(i.ToString());
                Assert.True(success);
            }

            Assert.Equal(count / 2, jObject.Count);

            // The new JSON contains half the entries.
            expectedJson = jObject.ToJsonString();

            // Add back every other entry (to the end)
            for (i = 0; i < count; i += 2)
            {
                jObject.Add(i.ToString(), JsonValue.Create(i));
            }
            Assert.Equal(count, jObject.Count);

            // The beginning part of the JSON should be the same.
            string json = jObject.ToJsonString();
            Assert.Contains(expectedJson.TrimEnd('}'), json);

            const int ItemsToAdd = 10;
            for (i = 10000; i < 10000 + ItemsToAdd; i++)
            {
                jObject.Add(i.ToString(), i);
            }

            // Original items should still be in front.
            json = jObject.ToJsonString();
            Assert.Contains(expectedJson.TrimEnd('}'), json);

            Assert.Equal(count + ItemsToAdd, jObject.Count);
        }

        public static IEnumerable<object[]> JObjectCollectionData()
        {
            // Ensure that the list-to-dictionary threshold is hit (currently 9).
            for (int i = 0; i < 25; i++)
            {
                yield return CreateArray(i);
                yield return CreateArray_JsonElement(i);
            }

            yield return CreateArray(123);
            yield return CreateArray_JsonElement(122);

            yield return CreateArray(300);
            yield return CreateArray_JsonElement(299);

            object[] CreateArray(int count)
            {
                JsonObject jObject = CreateJsonObject(count);
                return new object[] { jObject, count };
            }

            JsonObject CreateJsonObject(int count)
            {
                var jObject = new JsonObject();

                for (int i = 0; i < count; i++)
                {
                    jObject[i.ToString()] = i;
                }

                return jObject;
            }

            object[] CreateArray_JsonElement(int count)
            {
                JsonObject jObject = CreateJsonObject(count);
                string json = jObject.ToJsonString();
                jObject = JsonNode.Parse(json).AsObject();
                return new object[] { jObject, count };
            }
        }

        [Theory]
        [MemberData(nameof(JObjectCollectionData))]
        public static void ChangeCollectionWhileEnumeratingFails(JsonObject jObject, int count)
        {
            if (count == 0)
            {
                return;
            }

            Assert.Equal(count, jObject.Count);

            int index = 0;

            // Exception string sample: "Collection was modified; enumeration operation may not execute"
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (KeyValuePair<string, JsonNode?> node in jObject)
                {
                    index++;
                    jObject.Add("New_A", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (KeyValuePair<string, JsonNode?> node in jObject)
                {
                    index++;
                    jObject.Remove(node.Key);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            IEnumerable iEnumerable = jObject;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (KeyValuePair<string, JsonNode?> node in iEnumerable)
                {
                    index++;
                    jObject.Add("New_B", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (KeyValuePair<string, JsonNode?> node in iEnumerable)
                {
                    index++;
                    jObject.Remove(node.Key);
                }
            });
            Assert.Equal(1, index);

            IDictionary<string, JsonNode?> iDictionary = jObject;

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (string str in iDictionary.Keys)
                {
                    index++;
                    jObject.Add("New_C", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (string str in (IEnumerable)iDictionary.Keys)
                {
                    index++;
                    jObject.Add("New_D", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (string str in iDictionary.Keys)
                {
                    index++;
                    jObject.Remove(str);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (JsonNode node in iDictionary.Values)
                {
                    index++;
                    jObject.Add("New_E", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (JsonNode node in (IEnumerable)iDictionary.Values)
                {
                    index++;
                    jObject.Add("New_F", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (JsonNode node in iDictionary.Values)
                {
                    index++;
                    jObject.Clear();
                }
            });
            Assert.Equal(1, index);
        }

        [Fact]
        public static void TestJsonNodeOptionsSet()
        {
            var options = new JsonNodeOptions()
            {
                PropertyNameCaseInsensitive = true
            };

            // Ctor that takes just options
            var obj1 = new JsonObject(options);
            obj1["Hello"] = "World";

            // Ctor that takes props IEnumerable + options
            IEnumerable<KeyValuePair<string, JsonNode?>> props = new List<KeyValuePair<string, JsonNode?>>
            {
                new KeyValuePair<string, JsonNode?>("Hello", "World")
            };
            var obj2 = new JsonObject(props, options);

            // Create method
            using JsonDocument doc = JsonDocument.Parse(@"{""Hello"":""World""}");
            var obj3 = JsonObject.Create(doc.RootElement, options);

            Test(obj1);
            Test(obj2);
            Test(obj3);

            static void Test(JsonObject jObject)
            {
                Assert.NotNull(jObject.Options);
                Assert.True(jObject.Options.Value.PropertyNameCaseInsensitive);
                Assert.Equal("World", (string)jObject["Hello"]);
                Assert.Equal("World", (string)jObject["hello"]); // Test case insensitivity
            }
        }

        [Fact]
        public static void LazyInitializationIsThreadSafe()
        {
            string arrayText = "{\"prop0\":0,\"prop1\":1}";
            JsonObject jObj = Assert.IsType<JsonObject>(JsonNode.Parse(arrayText));
            Parallel.For(0, 128, i =>
            {
                Assert.Equal(0, (int)jObj["prop0"]);
                Assert.Equal(1, (int)jObj["prop1"]);
            });
        }

        [Fact]
        public static void DeepClone()
        {
            var array = new JsonArray();
            array.Add(5);
            array.Add(7);
            var nestedJsonObj = new JsonObject()
            {
                { "Ten", 10 },
                { "Name", "xyz"}
            };

            var jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["Two"] = 2;
            jObject["String"] = "ABC";
            jObject["True"] = true;
            jObject["False"] = false;
            jObject["Null"] = null;
            jObject["value"] = JsonValue.Create(10);
            jObject["array"] = array;
            jObject["object"] = nestedJsonObj;

            var clone = jObject.DeepClone().AsObject();

            JsonNodeTests.AssertDeepEqual(jObject, clone);

            Assert.Equal(jObject.Count, clone.Count);
            Assert.Equal(1, clone["One"].GetValue<int>());
            Assert.Equal(2, clone["Two"].GetValue<int>());
            Assert.Equal("ABC", clone["String"].GetValue<string>());
            Assert.True(clone["True"].GetValue<bool>());
            Assert.False(clone["False"].GetValue<bool>());
            Assert.Null(clone["Null"]);
            Assert.Equal(10, clone["value"].GetValue<int>());

            JsonArray clonedArray = clone["array"].AsArray();
            Assert.Equal(array.Count, clonedArray.Count);
            Assert.Equal(5, clonedArray[0].GetValue<int>());
            Assert.Equal(7, clonedArray[1].GetValue<int>());

            JsonObject clonedNestedJObject = clone["object"].AsObject();
            Assert.Equal(nestedJsonObj.Count, clonedNestedJObject.Count);
            Assert.Equal(10, clonedNestedJObject["Ten"].GetValue<int>());
            Assert.Equal("xyz", clonedNestedJObject["Name"].GetValue<string>());

            string originalJson = jObject.ToJsonString();
            string clonedJson = clone.ToJsonString();

            Assert.Equal(originalJson, clonedJson);
        }

        [Fact]
        public static void DeepClone_FromElement()
        {
            using JsonDocument document = JsonDocument.Parse("{\"One\": 1, \"String\": \"abc\"}");
            JsonObject jObject = JsonObject.Create(document.RootElement);
            var clone = jObject.DeepClone().AsObject();

            JsonNodeTests.AssertDeepEqual(jObject, clone);
            Assert.Equal(1, clone["One"].GetValue<int>());
            Assert.Equal("abc", clone["String"].GetValue<string>());
        }

        [Fact]
        public static void DeepEquals()
        {
            var jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["array"] = new JsonArray() { "a", "b" };

            var sameJObject = new JsonObject();
            sameJObject["One"] = 1;
            sameJObject["array"] = new JsonArray() { "a", "b" };

            JsonNodeTests.AssertDeepEqual(jObject, sameJObject);
            JsonNodeTests.AssertNotDeepEqual(jObject, null);

            var diffJObject = new JsonObject();
            diffJObject["One"] = 3;

            JsonNodeTests.AssertNotDeepEqual(diffJObject, jObject);
        }

        [Fact]
        public static void DeepEquals_JsonObject_With_JsonValuePOCO()
        {
            var jObject = new JsonObject();
            jObject["Id"] = 1;
            jObject["Name"] = "First";
            var nestedObject = new JsonObject();
            nestedObject["Id"] = 2;
            nestedObject["Name"] = "Last";
            nestedObject["NestedObject"] = null;
            jObject["NestedObject"] = nestedObject;

            var poco = new SimpleClass()
            {
                Id = 1,
                Name = "First",
                NestedObject = new SimpleClass()
                {
                    Id = 2,
                    Name = "Last",
                }
            };

            JsonNodeTests.AssertDeepEqual(jObject, JsonValue.Create(poco));

            var diffPoco = new SimpleClass()
            {
                Id = 1,
                Name = "First",
                NestedObject = new SimpleClass()
                {
                    Id = 3,
                    Name = "Last",
                }
            };

            JsonNodeTests.AssertNotDeepEqual(jObject, JsonValue.Create(diffPoco));
        }

        [Fact]
        public static void DeepEquals_JsonObject_With_Dictionary()
        {
            var jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["array"] = new JsonArray() { "a", "b" };
            jObject["obj"] = new JsonObject();

            var dictionary = new Dictionary<string, object>()
            {
                { "One", 1 },
                { "array", new string[] { "a", "b" } },
                { "obj", new { } }
            };

            JsonNodeTests.AssertDeepEqual(jObject, JsonValue.Create(dictionary));

            var diffDictionary = new Dictionary<string, object>()
            {
                { "One", 1 },
                { "array", new string[] { "a", "d" } },
                { "obj", new { } }
            };

            JsonNodeTests.AssertNotDeepEqual(jObject, JsonValue.Create(diffDictionary));
        }

        private class SimpleClass
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public SimpleClass? NestedObject { get; set; }
        }

        [Fact]
        public static void DeepEqualFromElement()
        {
            using JsonDocument document = JsonDocument.Parse("{\"One\": 1, \"String\": \"abc\"}");
            JsonObject jObject = JsonObject.Create(document.RootElement);

            using JsonDocument document2 = JsonDocument.Parse("{\"One\":     1, \"String\":     \"abc\"}   ");
            JsonObject jObject2 = JsonObject.Create(document2.RootElement);
            JsonNodeTests.AssertDeepEqual(jObject, jObject2);

            using JsonDocument document3 = JsonDocument.Parse("{\"One\": 3, \"String\": \"abc\"}");
            JsonObject jObject3 = JsonObject.Create(document3.RootElement);
            JsonNodeTests.AssertNotDeepEqual(jObject, jObject3);

            using JsonDocument document4 = JsonDocument.Parse("{\"One\":     1, \"String\":     \"abc2\"}   ");
            JsonObject jObject4 = JsonObject.Create(document4.RootElement);
            JsonNodeTests.AssertNotDeepEqual(jObject, jObject4);
        }

        [Fact]
        public static void UpdateClonedObjectNotAffectOriginal()
        {
            var jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["Two"] = 2;

            var clone = jObject.DeepClone().AsObject();
            clone["One"] = 3;

            Assert.Equal(1, jObject["One"].GetValue<int>());
        }

        [Fact]
        public static void GetValueKind()
        {
            Assert.Equal(JsonValueKind.Object, new JsonObject().GetValueKind());
        }

        [Fact]
        public static void GetPropertyName()
        {
            var jObject = new JsonObject();
            var jValue = JsonValue.Create(10);
            jObject.Add("value", jValue);

            Assert.Equal("value", jValue.GetPropertyName());
            Assert.Equal("value", jObject["value"].GetPropertyName());
        }

        [Fact]
        public static void ReplaceWith()
        {
            var jObject = new JsonObject();
            var jValue = JsonValue.Create(10);
            jObject["value"] = jValue;
            jObject["value"].ReplaceWith(5);

            Assert.Null(jValue.Parent);
            Assert.Equal("{\"value\":5}", jObject.ToJsonString());
        }

        // List-based APIs

        [Fact]
        public static void IntIndexer_Getter_ReturnsExpectedProperty()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            JsonNode? value;
            value = jObject[0];
            Assert.Equal(1, value.GetValue<int>());

            value = jObject[1];
            Assert.Equal("str", value.GetValue<string>());

            value = jObject[2];
            Assert.Null(value);
        }

        [Fact]
        public static void IntIndexer_Setter_UpdatesProperty()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Null(jObject["Three"]);
            Assert.Equal(3, jObject.Count);

            jObject[2] = -5;

            Assert.Equal(-5, jObject["Three"].GetValue<int>());
            Assert.Equal(3, jObject.Count);
        }

        [Fact]
        public static void IntIndexer_ArgumentOutOfRange_ThrowsException()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => jObject[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject[jObject.Count]);
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject[-1] = 42);
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject[jObject.Count] = 42);
        }

        [Fact]
        public static void GetAt_ReturnsExpectedProperty()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            KeyValuePair<string, JsonNode?> kvp;
            kvp = jObject.GetAt(0);
            Assert.Equal("One", kvp.Key);
            Assert.Equal(1, kvp.Value.GetValue<int>());

            kvp = jObject.GetAt(1);
            Assert.Equal("Two", kvp.Key);
            Assert.Equal("str", kvp.Value.GetValue<string>());

            kvp = jObject.GetAt(2);
            Assert.Equal("Three", kvp.Key);
            Assert.Null(kvp.Value);
        }

        [Fact]
        public static void GetAt_InvalidArgument_ThrowsArgumentOutOfRangeException()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.GetAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.GetAt(jObject.Count));
        }

        [Fact]
        public static void SetAt_UpdatesProperty()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Null(jObject["Three"]);
            Assert.Equal(3, jObject.Count);

            jObject.SetAt(2, -5);

            Assert.Equal(-5, jObject["Three"].GetValue<int>());
            Assert.Equal(3, jObject.Count);

            jObject.SetAt(2, "Three", -33);

            Assert.Equal(-33, jObject["Three"].GetValue<int>());
            Assert.Equal(3, jObject.Count);

            jObject.SetAt(2, "Four", "str");

            Assert.Equal(3, jObject.Count);
            Assert.DoesNotContain("Three", jObject);
            Assert.Contains("Four", jObject);
            Assert.Equal("str", jObject["Four"].GetValue<string>());
        }

        [Fact]
        public static void SetAt_CaseInsensitive_UpdatesProperty()
        {
            JsonObject jObject = new(new() { PropertyNameCaseInsensitive = true })
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Null(jObject["three"]);
            Assert.Equal(3, jObject.Count);

            jObject.SetAt(2, -5);

            Assert.Equal(-5, jObject["three"].GetValue<int>());
            Assert.Equal(3, jObject.Count);

            jObject.SetAt(2, "THREE", -33);

            Assert.Equal(-33, jObject["ThRee"].GetValue<int>());
            Assert.Equal(3, jObject.Count);

            jObject.SetAt(2, "Four", "str");

            Assert.Equal(3, jObject.Count);
            Assert.DoesNotContain("Three", jObject);
            Assert.Contains("FOUR", jObject);
            Assert.Equal("str", jObject["FoUR"].GetValue<string>());
        }

        [Fact]
        public static void SetAt_InvalidArgument_ThrowsArgumentOutOfRangeException()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            // Throws exception if the property name is null.
            Assert.Throws<ArgumentNullException>(() => jObject.SetAt(2, propertyName: null, 5));

            // Throws exception if the index is out of range.
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.SetAt(-1, "Four", 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.SetAt(jObject.Count, "Four", 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.SetAt(-1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.SetAt(jObject.Count, 5));

            // Throws exception if the key exists at a different position.
            Assert.Throws<ArgumentException>(() => jObject.SetAt(2, "One", "str"));
        }

        [Fact]
        public static void IndexOf_ReturnsExpectedIndex()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Equal(0, jObject.IndexOf("One"));
            Assert.Equal(1, jObject.IndexOf("Two"));
            Assert.Equal(2, jObject.IndexOf("Three"));

            Assert.Equal(-1, jObject.IndexOf("Four"));
            Assert.Equal(-1, jObject.IndexOf("three"));
        }

        [Fact]
        public static void IndexOf_CaseInsensitive_ReturnsExpectedIndex()
        {
            JsonObject jObject = new(new() { PropertyNameCaseInsensitive = true })
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Equal(0, jObject.IndexOf("One"));
            Assert.Equal(1, jObject.IndexOf("Two"));
            Assert.Equal(2, jObject.IndexOf("Three"));

            Assert.Equal(0, jObject.IndexOf("onE"));
            Assert.Equal(1, jObject.IndexOf("tWo"));
            Assert.Equal(2, jObject.IndexOf("THREE"));

            Assert.Equal(-1, jObject.IndexOf("Four"));
        }

        [Fact]
        public static void IndexOf_NullPropertyName_ThrowsArgumentNullException()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Throws<ArgumentNullException>(() => jObject.IndexOf(propertyName: null));
        }

        [Fact]
        public static void Insert_InsertsProperty()
        {
            JsonObject jObject = new()
            {
                ["Two"] = "str",
            };

            Assert.Equal(1, jObject.Count);

            jObject.Insert(0, "One", 1);

            Assert.Equal(2, jObject.Count);
            Assert.Equal(1, jObject["One"].GetValue<int>());
            Assert.Equal("str", jObject["Two"].GetValue<string>());
            Assert.Equal(0, jObject.IndexOf("One"));
            Assert.Equal(1, jObject.IndexOf("Two"));

            jObject.Insert(1, "Three", null);

            Assert.Equal(3, jObject.Count);
            Assert.Equal(1, jObject["One"].GetValue<int>());
            Assert.Null(jObject["Three"]);
            Assert.Equal("str", jObject["Two"].GetValue<string>());
            Assert.Equal(0, jObject.IndexOf("One"));
            Assert.Equal(1, jObject.IndexOf("Three"));
            Assert.Equal(2, jObject.IndexOf("Two"));

            jObject.Insert(jObject.Count, "Four", 4);

            Assert.Equal(4, jObject.Count);
            Assert.Equal(1, jObject["One"].GetValue<int>());
            Assert.Null(jObject["Three"]);
            Assert.Equal("str", jObject["Two"].GetValue<string>());
            Assert.Equal(4, jObject["Four"].GetValue<int>());
            Assert.Equal(0, jObject.IndexOf("One"));
            Assert.Equal(1, jObject.IndexOf("Three"));
            Assert.Equal(2, jObject.IndexOf("Two"));
            Assert.Equal(3, jObject.IndexOf("Four"));
        }

        [Fact]
        public static void Insert_ExistingKey_ThrowsArgumentException()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Equal(3, jObject.Count);

            Assert.Throws<ArgumentException>(() => jObject.Insert(1, "Three", 3));

            Assert.Equal(3, jObject.Count);
        }

        [Fact]
        public static void Insert_NullKey_ThrowsArgumentNullException()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Equal(3, jObject.Count);

            Assert.Throws<ArgumentNullException>(() => jObject.Insert(1, propertyName: null, 3));

            Assert.Equal(3, jObject.Count);
        }

        [Fact]
        public static void Insert_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Equal(3, jObject.Count);

            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.Insert(-1, "Four", 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.Insert(jObject.Count + 1, "Four", 4));

            Assert.Equal(3, jObject.Count);
        }

        [Fact]
        public static void RemoveAt_RemovesProperty()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            jObject.RemoveAt(1);

            Assert.Equal(2, jObject.Count);
            Assert.Contains("One", jObject);
            Assert.DoesNotContain("Two", jObject);
            Assert.Contains("Three", jObject);

            jObject.RemoveAt(1);

            Assert.Equal(1, jObject.Count);
            Assert.Contains("One", jObject);
            Assert.DoesNotContain("Two", jObject);
            Assert.DoesNotContain("Three", jObject);

            jObject.RemoveAt(0);

            Assert.Empty(jObject);
            Assert.DoesNotContain("One", jObject);
            Assert.DoesNotContain("Two", jObject);
            Assert.DoesNotContain("Three", jObject);
        }

        [Fact]
        public static void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.RemoveAt(jObject.Count));
        }

        [Fact]
        public static void JsonObject_IsIList()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            IList<KeyValuePair<string, JsonNode?>> ilist = Assert.IsAssignableFrom<IList<KeyValuePair<string, JsonNode?>>>(jObject);

            // Indexer getter
            KeyValuePair<string, JsonNode?> kvp = ilist[1];
            Assert.Equal("Two", kvp.Key);
            Assert.Equal("str", kvp.Value.GetValue<string>());

            // Indexer setter
            kvp = new("Four", 4);
            ilist[1] = kvp;
            Assert.Contains(kvp, ilist);
            Assert.DoesNotContain("Two", jObject);
            Assert.Contains("Four", jObject);

            // IndexOf
            Assert.Equal(1, ilist.IndexOf(kvp));
            Assert.Equal(-1, ilist.IndexOf(new("Four", 4))); // Different JsonNode instance

            // Insert
            ilist.Insert(1, new("Two", "str"));
            Assert.Equal(4, ilist.Count);
            Assert.Contains("Two", jObject);
            Assert.Equal(1, jObject.IndexOf("Two"));

            // RemoveAt
            ilist.RemoveAt(1);
            Assert.Equal(3, ilist.Count);
            Assert.DoesNotContain("Two", jObject);
            Assert.Equal(-1, jObject.IndexOf("Two"));
        }

        [Fact]
        public static void TryAdd_NewKey_EmptyJsonObject()
        {
            JsonObject jObject = new();

            Assert.True(jObject.TryAdd("First", "value", out var index));
            Assert.Equal(0, index);
        }

        [Fact]
        public static void TryAdd_NewKey()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.True(jObject.TryAdd("Four", 33, out var index));
            Assert.Equal(3, index);
        }

        [Fact]
        public static void TryAdd_ExistingKey()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.False(jObject.TryAdd("Two", 33, out var index));
            Assert.Equal(1, index);
        }

        [Fact]
        public static void TryAdd_ThrowsArgumentNullException()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Throws<ArgumentNullException>(() => { jObject.TryAdd(null, 33); });
        }

        [Fact]
        public static void TryGetPropertyValue_ThrowsArgumentNullException()
        {
            JsonObject jObject = new()
            {
                ["One"] = 1,
                ["Two"] = "str",
                ["Three"] = null,
            };

            Assert.Throws<ArgumentNullException>(() => { jObject.TryGetPropertyValue(null, out var jsonNode, out var index); });
        }

        [Theory]
        [InlineData(10_000)]
        [InlineData(50_000)]
        [InlineData(100_000)]
        public static void JsonObject_ExtensionData_ManyDuplicatePayloads(int size)
        {
            // Generate the payload
            StringBuilder builder = new StringBuilder();
            builder.Append("{");
            for (int i = 0; i < size; i++)
            {
                builder.Append($"\"{i}\": 0,");
                builder.Append($"\"{i}\": 0,");
            }
            builder.Length--; // strip trailing comma
            builder.Append("}");

            string jsonPayload = builder.ToString();
            ClassWithObjectExtensionData result = JsonSerializer.Deserialize<ClassWithObjectExtensionData>(jsonPayload);
            Assert.Equal(size, result.ExtensionData.Count);
        }

        class ClassWithObjectExtensionData
        {
            [JsonExtensionData]
            public JsonObject ExtensionData { get; set; }
        }

        [Theory]
        [MemberData(nameof(TestData.DuplicatePropertyJsonPayloads), MemberType = typeof(TestData))]
        public static void JsonObject_DuplicatePropertyThrows(string jsonPayload, bool isValidJson = false)
        {
            AssertDuplicatePropertyThrows<JsonObject>(jsonPayload, isValidJson);
            AssertDuplicatePropertyThrows<JsonNode>(jsonPayload, isValidJson);
        }

        [Theory]
        [MemberData(nameof(TestData.DuplicatePropertyJsonPayloads), MemberType = typeof(TestData))]
        public static void JsonObject_DuplicatePropertyThrows_NestedInArray(string jsonPayload, bool isValidJson = false)
        {
            jsonPayload = $"[{jsonPayload}]";
            AssertDuplicatePropertyThrows<JsonArray>(jsonPayload, isValidJson);
            AssertDuplicatePropertyThrows<JsonNode>(jsonPayload, isValidJson);
        }

        [Theory]
        [MemberData(nameof(TestData.DuplicatePropertyJsonPayloads), MemberType = typeof(TestData))]
        public static void JsonObject_DuplicatePropertyThrows_NestedDeeply(string jsonPayload, bool isValidJson = false)
        {
            jsonPayload = $$"""{"p0":{"p1":{"p2":{"p3":{"p4":{"p5":{"p6":{"p7":{"p8":{"p9":{{jsonPayload}}} } } } } } } } } }""";
            AssertDuplicatePropertyThrows<JsonObject>(jsonPayload, isValidJson);
            AssertDuplicatePropertyThrows<JsonNode>(jsonPayload, isValidJson);
        }

        private static void AssertDuplicatePropertyThrows<T>(string jsonPayload, bool isValidJson)
            where T : JsonNode
        {
            if (isValidJson)
            {
                T node = JsonSerializer.Deserialize<T>(jsonPayload, JsonTestSerializerOptions.DisallowDuplicateProperties);
                JsonNode.DeepEquals(node, node); // Assert no throw

                node = JsonSerializer.Deserialize<T>(jsonPayload);
                JsonNode.DeepEquals(node, node); // Assert no throw
            }
            else
            {
                AssertExtensions.ThrowsContains<ArgumentException>(
                    () => JsonSerializer.Deserialize<T>(jsonPayload, JsonTestSerializerOptions.DisallowDuplicateProperties),
                    "An item with the same key has already been added.");

                // Default options don't throw on deserialize but will throw when accessed
                T node = JsonSerializer.Deserialize<T>(jsonPayload);
                AssertExtensions.ThrowsContains<ArgumentException>(
                    () => JsonNode.DeepEquals(node, node),
                    "An item with the same key has already been added.");
            }
        }

        [Fact]
        public static void JsonObject_DuplicatePropertyCaseInsensitiveThrows()
        {
            JsonSerializerOptions options = JsonTestSerializerOptions.DisallowDuplicatePropertiesIgnoringCase;

            string jsonPayload = """{"a":1,"A":2}""";

            _ = JsonSerializer.Deserialize<JsonObject>(jsonPayload); // Assert no throw
            AssertExtensions.ThrowsContains<ArgumentException>(
                () => JsonSerializer.Deserialize<JsonObject>(jsonPayload, options),
                "An item with the same key has already been added.");

            _ = JsonSerializer.Deserialize<JsonNode>(jsonPayload); // Assert no throw
            AssertExtensions.ThrowsContains<ArgumentException>(
                () => JsonSerializer.Deserialize<JsonNode>(jsonPayload, options),
                "An item with the same key has already been added.");
        }

        [Fact]
        public static void JsonObject_NestedDuplicatePropertyCaseInsensitiveThrows()
        {
            JsonSerializerOptions options = JsonTestSerializerOptions.DisallowDuplicatePropertiesIgnoringCase;

            string jsonPayload = """[{"a":1,"A":2}]""";

            _ = JsonSerializer.Deserialize<JsonArray>(jsonPayload); // Assert no throw
            AssertExtensions.ThrowsContains<ArgumentException>(
                () => JsonSerializer.Deserialize<JsonArray>(jsonPayload, options),
                "An item with the same key has already been added.");

            _ = JsonSerializer.Deserialize<JsonNode>(jsonPayload); // Assert no throw
            AssertExtensions.ThrowsContains<ArgumentException>(
                () => JsonSerializer.Deserialize<JsonNode>(jsonPayload, options),
                "An item with the same key has already been added.");
        }

        [Theory]
        [InlineData("42")]
        [InlineData("[]")]
        public static void Deserialize_WrongType(string json)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JsonObject>(json));
        }
    }
}
