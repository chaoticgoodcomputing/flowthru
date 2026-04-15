// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Apache.Arrow;
using Flowthru.Spark;
using Flowthru.Spark.Sql;
using Microsoft.Data.Analysis;
using Xunit;
using static Flowthru.Spark.Utils.CommandSerDe;
using static Flowthru.Tests.Spark.TestUtils.ArrowTestUtils;
using RDDWorkerFunction = Flowthru.Spark.RDD.WorkerFunction;

namespace Flowthru.Tests.Spark
{
    [Collection("Spark Unit Tests")]
    public class CommandSerDeTests
    {
        [Fact]
        public void TestCommandSerDeForSqlPickling()
        {
            var udfWrapper = new PicklingUdfWrapper<string, string>((str) => $"hello {str}");
            var workerFunction = new PicklingWorkerFunction(udfWrapper.Execute);

            byte[] serializedCommand = Serialize(
              workerFunction.Func,
              SerializedMode.Row,
              SerializedMode.Row
            );

            using var ms = new MemoryStream(serializedCommand);
            var deserializedWorkerFunction = new PicklingWorkerFunction(
              Deserialize<PicklingWorkerFunction.ExecuteDelegate>(
                ms,
                out SerializedMode serializerMode,
                out SerializedMode deserializerMode,
                out var runMode
              )
            );

            Assert.Equal(SerializedMode.Row, serializerMode);
            Assert.Equal(SerializedMode.Row, deserializerMode);
            Assert.Equal("N", runMode);

            object result = deserializedWorkerFunction.Func(0, new[] { "spark" }, new[] { 0 });
            Assert.Equal("hello spark", result);
        }

        [Fact]
        public void TestCommandSerDeForSqlArrow()
        {
            var udfWrapper = new ArrowUdfWrapper<StringArray, StringArray>(
              (strings) =>
                (StringArray)ToArrowArray(
                  Enumerable
                    .Range(0, strings.Length)
                    .Select(i => $"hello {strings.GetString(i)}")
                    .ToArray()
                )
            );

            var workerFunction = new ArrowWorkerFunction(udfWrapper.Execute);

            byte[] serializedCommand = Serialize(
              workerFunction.Func,
              SerializedMode.Row,
              SerializedMode.Row
            );

            using var ms = new MemoryStream(serializedCommand);
            var deserializedWorkerFunction = new ArrowWorkerFunction(
              Deserialize<ArrowWorkerFunction.ExecuteDelegate>(
                ms,
                out SerializedMode serializerMode,
                out SerializedMode deserializerMode,
                out var runMode
              )
            );

            Assert.Equal(SerializedMode.Row, serializerMode);
            Assert.Equal(SerializedMode.Row, deserializerMode);
            Assert.Equal("N", runMode);

            IArrowArray input = ToArrowArray(new[] { "spark" });
            IArrowArray result = deserializedWorkerFunction.Func(new[] { input }, new[] { 0 });
            AssertEquals("hello spark", result);
        }

        [Fact]
        public void TestCommandSerDeForSqlArrowDataFrame()
        {
            var udfWrapper = new DataFrameUdfWrapper<
              ArrowStringDataFrameColumn,
              ArrowStringDataFrameColumn
            >((strings) => strings.Apply(cur => $"hello {cur}"));

            var workerFunction = new DataFrameWorkerFunction(udfWrapper.Execute);

            byte[] serializedCommand = Serialize(
              workerFunction.Func,
              SerializedMode.Row,
              SerializedMode.Row
            );

            using var ms = new MemoryStream(serializedCommand);
            var deserializedWorkerFunction = new DataFrameWorkerFunction(
              Deserialize<DataFrameWorkerFunction.ExecuteDelegate>(
                ms,
                out SerializedMode serializerMode,
                out SerializedMode deserializerMode,
                out var runMode
              )
            );

            Assert.Equal(SerializedMode.Row, serializerMode);
            Assert.Equal(SerializedMode.Row, deserializerMode);
            Assert.Equal("N", runMode);

            var column = (StringArray)ToArrowArray(new[] { "spark" });

            ArrowStringDataFrameColumn ArrowStringDataFrameColumn = ToArrowStringDataFrameColumn(column);
            DataFrameColumn result = deserializedWorkerFunction.Func(
              new[] { ArrowStringDataFrameColumn },
              new[] { 0 }
            );
            AssertEquals("hello spark", result);
        }

        [Fact]
        public void TestCommandSerDeForRDD()
        {
            // Construct the UDF tree such that func1, func2, and func3
            // are executed in that order.
            var func1 = new RDDWorkerFunction(new RDD<int>.MapUdfWrapper<int, int>((a) => a + 3).Execute);

            var func2 = new RDDWorkerFunction(new RDD<int>.MapUdfWrapper<int, int>((a) => a * 2).Execute);

            var func3 = new RDDWorkerFunction(new RDD<int>.MapUdfWrapper<int, int>((a) => a + 5).Execute);

            RDDWorkerFunction chainedFunc1 = RDDWorkerFunction.Chain(func1, func2);
            RDDWorkerFunction chainedFunc2 = RDDWorkerFunction.Chain(chainedFunc1, func3);

            byte[] serializedCommand = Serialize(
              chainedFunc2.Func,
              SerializedMode.Byte,
              SerializedMode.Byte
            );

            using var ms = new MemoryStream(serializedCommand);
            var deserializedWorkerFunction = new RDDWorkerFunction(
              Deserialize<RDDWorkerFunction.ExecuteDelegate>(
                ms,
                out SerializedMode serializerMode,
                out SerializedMode deserializerMode,
                out var runMode
              )
            );

            Assert.Equal(SerializedMode.Byte, serializerMode);
            Assert.Equal(SerializedMode.Byte, deserializerMode);
            Assert.Equal("N", runMode);

            IEnumerable<object> result = deserializedWorkerFunction.Func(0, new object[] { 1, 2, 3 });
            Assert.Equal(new[] { 13, 15, 17 }, result.Cast<int>());
        }
    }
}
