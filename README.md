ConfigParser
[![Build status](https://ci.appveyor.com/api/projects/status/08aiy2tgs7n3y2fg?svg=true)](https://ci.appveyor.com/project/salaros/configparser)
[![AppVeyor tests branch](https://img.shields.io/appveyor/tests/salaros/configparser/master.svg)](https://ci.appveyor.com/project/salaros/configparser/build/tests)
[![Coverage Status](https://coveralls.io/repos/github/salaros/ConfigParser/badge.svg?branch=master)](https://coveralls.io/github/salaros/ConfigParser?branch=master)
=============

![GitHub top language](https://img.shields.io/github/languages/top/salaros/ConfigParser.svg?colorB=333333)
[![.NET Standard](https://img.shields.io/badge/cross%20platform-yes-45a234.svg)](https://en.wikipedia.org/wiki/Cross-platform)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0+-484877.svg)](https://social.msdn.microsoft.com/Forums/vstudio/en-US/7035edc6-97fc-49ee-8eee-2fa4d040a63b/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0+-748478.svg)](https://social.msdn.microsoft.com/Forums/vstudio/en-US/7035edc6-97fc-49ee-8eee-2fa4d040a63b/)

[![License](https://img.shields.io/github/license/salaros/configparser.svg)](https://github.com/salaros/configparser/blob/master/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Salaros.ConfigParser.svg?label=NuGet&colorA=004880&colorB=CFC76B)](https://www.nuget.org/packages/Salaros.ConfigParser)
[![NuGet Pre Release](https://img.shields.io/nuget/vpre/Salaros.ConfigParser.svg?label=NuGet%20pre-release&colorA=504880&colorB=CFC76B)](https://www.nuget.org/packages/Salaros.ConfigParser)
[![NuGet](https://img.shields.io/nuget/dt/Salaros.ConfigParser.svg?colorA=004880&colorB=CFC76B)](https://www.nuget.org/packages/Salaros.ConfigParser)

**ConfigParser** - is a slim, cross-platform, fully managed C# library for reading and writing .ini, .conf, .cfg etc configuration files.

You could use it in your [Unity 3D](https://unity3d.com/), [Xamarin](http://xamarin.com) (iOS & Android), .NET Framework applications (even with old 4.0/4.5), .NET Core CLI and ASP.NET Core applications, [Mono](https://www.mono-project.com/) etc

Features
========

## Customization

- [x] customizable encoding (most encodings can be auto-detected)
- [x] customizable culture
- [x] customizable number styles (e.g. currencies, exponential notation etc)
- [x] customizable line endings (usually auto-detected)
- [x] customizable true and false (e.g. "verum" / "falsum" )
- [x] customizable comment characters
- [x] customizable key/value separator (defaults to '=')

## Read and preserved

- [x] comment lines
- [x] section comments
- [x] empty lines
- [x] indented sections
- [x] indented keys
- [x] indented values

## Values

- [x] default values
- [x] multi-line values (both quoted and not)
- [x] quoted values
- [x] null values (value-less keys)
- [x] array values
- [x] fancy float / double
- [x] byte-encoded values
- [x] smart boolean values (0/1, on/off, enabled/disabled work of the box)

and more...

Installation
============

**RevitLESS Toolkit** can be installed via [NuGet](https://www.nuget.org/packages/Salaros.ConfigParser)
by using Package Manager in your IDE, `dotnet` binary or Package Console

```bash
# Add the Salaros.ConfigParser package to a project named [<PROJECT>]
dotnet add [<PROJECT>] package Salaros.ConfigParser
```

or Visual Studio's Package Console

```powershell
# Add the Salaros.ConfigParser package to the default project
Install-Package Salaros.ConfigParser

# Add the Salaros.ConfigParser package to a project named [<PROJECT>]
Install-Package Salaros.ConfigParser -ProjectName [<PROJECT>]
```

Usage
=====

```csharp
// Initialize config file instance from file
var configFileFromPath = new ConfigParser(@"path\to\configfile.cnf");

// Parse text
var configFileFromString = new ConfigParser(@"
    [Strings]
        canBeIndented = value
    andQuoted = ""quotes will be stripped""

    [Numbers]
    withD = 0.6D
    dollars = $2,999

    [boolean]
    numericTrue = 1
    textFalse = true
    yesWorks = yes
    upperCaseWorks = on
    worksAsWell = Enabled

    [Advanced]
    arrayWorkToo =
        arrayElement1
        arrayElement2
    valueLessKey",
    new ConfigParserSettings
    {
        MultiLineValues = MultiLineValues.Simple | MultiLineValues.AllowValuelessKeys | MultiLineValues.QuoteDelimitedValues,
        Culture = new CultureInfo("en-US")
    }
);

configFileFromString.GetValue("Strings", "canBeIndented");          // value
configFileFromString["Strings"]["canBeIndented"];                   // returns 'value' too
configFileFromString.GetValue("Strings", "andQuoted");              // quotes will be stripped

configFileFromString.GetValue("Numbers", "withD", 0D);              // 0,6
configFileFromString.GetValue("Numbers", "dollars", 0D,             // 2999
    NumberStyles.AllowCurrencySymbol);
configFileFromString.GetValue("Numbers", "dollars");                // $2,999

configFileFromString.GetValue("boolean", "numericTrue", false);     // True
configFileFromString.GetValue("boolean", "textFalse", false);       // True
configFileFromString.GetValue("boolean", "yesWorks", false);        // True
configFileFromString.GetValue("boolean", "upperCaseWorks", false);  // True
configFileFromString.GetValue("boolean", "worksAsWell", false);     // True

configFileFromString.GetArrayValue("Advanced", "arrayWorkToo");     // ["arrayElement1","arrayElement2"]
configFileFromString.GetValue("Advanced", "valueLessKey");          //
```

How to build
============

You need Git and [.NET Core SDK](https://www.microsoft.com/net/download/)

```bash
git clone https://github.com/salaros/ConfigParser
cd ConfigParser
dotnet build
```

How to test
===========

**ConfigParser** uses [xUnit.net](https://xunit.github.io/), so you can run unit tests by simply using the following command:

```bash
dotnet test tests
```

License
=======

**ConfigParser** is distributed under the [MIT license](LICENSE), which grants you

- [x] Private use
- [x] Commercial use
- [x] Modification
- [x] Distribution

However you have to include the content of [license](LICENSE) file in your source code (if you distribute your Software in text form), otherwise include it in your own LICENSE file or to some sort of **About -> Open-source libraries** section if you distribute your Software as a compiled library / binary.
Here is why (part of [MIT license](LICENSE)):

```
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
```