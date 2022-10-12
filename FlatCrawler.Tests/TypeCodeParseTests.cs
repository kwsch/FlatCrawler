using System;
using FlatCrawler.Lib;
using Xunit;
using FluentAssertions;

namespace FlatCrawler.Tests;

public static class TypeCodeParseTests
{
    [Theory]
    [InlineData("table", TypeCode.Object, true)]
    [InlineData("object", TypeCode.Object)]
    [InlineData("Object", TypeCode.Object)]
    [InlineData("uint", TypeCode.UInt32)]
    [InlineData("string", TypeCode.String)]
    [InlineData("u8", TypeCode.Byte)]
    [InlineData("S16", TypeCode.Int16)]
    [InlineData("FLOAT", TypeCode.Single)]
    public static void ParseTypeCode(string input, TypeCode type, bool alwaysArray = false)
    {
        var result = TypeCodeUtil.GetTypeCodeTuple(input);
        result.Type.Should().Be(type);
        result.AsArray.Should().Be(alwaysArray);

        var arr = $"{input}[]";
        result = TypeCodeUtil.GetTypeCodeTuple(arr);
        result.Type.Should().Be(type);
        result.AsArray.Should().Be(true);
    }
}
