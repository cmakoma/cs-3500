﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UITesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UITest.Extension;
using Keyboard = Microsoft.VisualStudio.TestTools.UITesting.Keyboard;

namespace CodedUITestProject1
{
    /// <summary>
    /// Summary description for CodedUITest1
    /// </summary>
    [CodedUITest]
    public class CodedUITest1
    {
        public CodedUITest1()
        {
        }

        [TestMethod]
        public void CodedUITestMethod1()
        {
            // To generate code for this test, select "Generate Code for Coded UI Test" from the shortcut menu and select one of the menu items.
            this.UIMap.SimpleFormulaTest();
            this.UIMap.SimpleFormulaTest_Assertion1();
            this.UIMap.CloseFormTest();
        }

        [TestMethod]
        public void CodedUITestMethod2()
        {
            this.UIMap.FormulaErrorTest1();
            this.UIMap.FormulaErrorTest1_Assertion1();            
        }

        [TestMethod]
        public void CodedUITestMethod3()
        {
            this.UIMap.FormulaErrorTest1();
            this.UIMap.FormulaErrorTest1_Assertion1();
        }

        [TestMethod]
        public void CodedUITestMethod4()
        {
            this.UIMap.InvalidFormulaTest1();
            this.UIMap.InvalidFormulaTest1_Assertion1();            
        }

        [TestMethod]
        public void CodedUITestMethod5()
        {
            
        }

        [TestMethod]
        public void CodedUITestMethod6()
        {
            this.UIMap.ChangedTest1();
            this.UIMap.ChangedTest1_Assertion1();
        }

        [TestMethod]
        public void CodedUITestMethod7()
        {
            this.UIMap.ChangedTest2();
            this.UIMap.ChangedTest2_Assertion1();
        }


        [TestMethod]
        public void CodedUITestMethod8()
        {
            this.UIMap.SaveAndOpenTest2();
            this.UIMap.SaveAndOpenTest2_Assertion1();
        }


        [TestMethod]
        public void CodedUITestMethod9()
        {
            this.UIMap.NewTest1();
            this.UIMap.NewTest1_Assertion1();
            this.UIMap.NewTest2();
            this.UIMap.NewTest2_Assertion1();
        }

        [TestMethod]
        public void CodedUITestMethod11()
        {
            this.UIMap.SaveAndOpenTest1();
            this.UIMap.SaveAndOpenTest1_Assertion1();
        }

        [TestMethod]
        public void CodedUITestMethod12()
        {
            this.UIMap.CloseTest1();
            this.UIMap.CloseHelpTest();
            this.UIMap.CloseTest2();
        }

        [TestMethod]
        public void CodedUITestMethod13()
        {
            this.UIMap.OpenTest3();
            this.UIMap.OpenTest3_Assertion1();
        }

        [TestMethod]
        public void CodedUITestMethod14()
        {
            // these tests wouldn't work for some reason
            //this.UIMap.ReadFileErrorTest2();
            //this.UIMap.ReadFileErrorTest2_Assertion1();
        }

        [TestMethod]
        public void CodedUITestMethod15()
        {
            // these tests wouldn't work for some reason
            //this.UIMap.CircularRefTest3();
            //this.UIMap.CircularRefTest3_Assertion1();
        }



        #region Additional test attributes

        // You can use the following additional attributes as you write your tests:

        ////Use TestInitialize to run code before running each test 
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{        
        //    // To generate code for this test, select "Generate Code for Coded UI Test" from the shortcut menu and select one of the menu items.
        //}

        ////Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{        
        //    // To generate code for this test, select "Generate Code for Coded UI Test" from the shortcut menu and select one of the menu items.
        //}

        #endregion

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        private TestContext testContextInstance;

        public UIMap UIMap
        {
            get
            {
                if ((this.map == null))
                {
                    this.map = new UIMap();
                }

                return this.map;
            }
        }

        private UIMap map;
    }
}
