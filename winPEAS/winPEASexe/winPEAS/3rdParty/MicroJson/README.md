MicroJson
=========

[![Version](https://img.shields.io/nuget/v/MicroJson.svg)](https://www.nuget.org/packages/MicroJson)
[![Build status](https://ci.appveyor.com/api/projects/status/d71toeu341rclabr/branch/master?svg=true)](https://ci.appveyor.com/project/mganss/microjson/branch/master)
[![Coverage Status](https://coveralls.io/repos/mganss/MicroJson/badge.svg?branch=master&service=github)](https://coveralls.io/github/mganss/MicroJson?branch=master)

MicroJson is a small library for serializing/deserializing [JSON](http://www.json.org/) strings from strongly 
typed CLR objects (POCOs). It is basically a replacement for the [JavaScriptSerializer](http://msdn.microsoft.com/en-us/library/system.web.script.serialization.javascriptserializer.aspx)
class for situations where you cannot or do not want to use that class. Its aim is neither to be fast nor feature-rich but
rather small and compatible with most environments.
If you are looking for a more fully featured JSON serializer, you should probably take a look at [JSON.Net](http://json.codeplex.com/).

MicroJson consists of two source files you can just drop into your project. It has been tested in the following environments:

- .NET 4.0
- [Mono](http://www.mono-project.com/Main_Page) 2.10
- [MonoTouch](http://www.xamarin.com) ([Touch.Unit](https://github.com/spouliot/Touch.Unit) tests included)
- [Mono for Android](http://www.xamarin.com) ([Andr.Unit](https://github.com/spouliot/Andr.Unit) tests included)

Usage
-----

	public class Test
    {
        public string S { get; set; }
        public int I { get; set; }
        public List<int> L;
    }

    var json = @"{
        ""S"": ""Hello, world."",
        ""I"": 4711,
        ""L"": [1, 2, 3]
    }";
    
    var t = new JsonSerializer().Deserialize<Test>(json);
    
    Assert.That(t.S, Is.EqualTo("Hello, world."));
    Assert.That(4711, Is.EqualTo(t.I));
    Assert.That(new[] { 1, 2, 3 }, Is.EquivalentTo(t.L));
    
    var j = new JsonSerializer().Serialize(t);
    
    Assert.That(j, Is.EqualTo(@"{""I"":4711,""L"":[1,2,3],""S"":""Hello, world.""}"));

Notes
-----

Deserialization is a two step process. First, JSON text is deserialized into generic CLR objects, i.e.
JSON arrays into `List<object>` and JSON objects into `Dictionary<object>`. If you only need this, then you can
just include `JsonParser.cs`.

Type information can be preserved when de/serializing by setting the `UseTypeInfo` property to true on the `JsonSerializer` object.
This will emit the class name of a serialized object as an additional property (can be configured through the property `TypeInfoPropertyName`, default is `@type`)
for classes which are derived and/or implement an interface.

License
-------

[MIT X11](http://en.wikipedia.org/wiki/MIT_License)

Used at
---------

[UpdateStar](http://www.updatestar.com/)
