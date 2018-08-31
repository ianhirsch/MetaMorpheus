﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TaskLayer;

namespace Test
{
    class CalibrationTests
    {
        [Test]
        public static void RunCalibrationEngineTest()
        {
            CalibrationTask calibrationTask = new CalibrationTask();
            string outputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestCalibration");
            string myFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\SmallCalibratible_Yeast.mzML");
            string myDatabase = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\smalldb.fasta");
            var engine = new EverythingRunnerEngine(new List<(string, MetaMorpheusTask)> { ( "Calibration", calibrationTask ) }, new List<string> { myFile }, new List<DbForTask> { new DbForTask(myDatabase, false) }, outputFolder);
            engine.Run();
        }
    }
}
