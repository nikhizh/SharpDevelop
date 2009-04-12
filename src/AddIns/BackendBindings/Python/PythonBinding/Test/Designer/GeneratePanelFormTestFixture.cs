// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using ICSharpCode.PythonBinding;
using NUnit.Framework;
using PythonBinding.Tests.Utils;

namespace PythonBinding.Tests.Designer
{
	[TestFixture]
	public class GeneratePanelFormTestFixture
	{
		string generatedPythonCode;
		
		[TestFixtureSetUp]
		public void SetUpFixture()
		{
			using (DesignSurface designSurface = new DesignSurface(typeof(Form))) {
				IDesignerHost host = (IDesignerHost)designSurface.GetService(typeof(IDesignerHost));
				IEventBindingService eventBindingService = new MockEventBindingService(host);
				Form form = (Form)host.RootComponent;			
				form.ClientSize = new Size(284, 264);
				
				PropertyDescriptorCollection descriptors = TypeDescriptor.GetProperties(form);
				PropertyDescriptor namePropertyDescriptor = descriptors.Find("Name", false);
				namePropertyDescriptor.SetValue(form, "MainForm");
				
				Panel panel = (Panel)host.CreateComponent(typeof(Panel), "panel1");
				panel.Location = new Point(10, 15);
				panel.TabIndex = 0;
				panel.Size = new Size(100, 120);
				TextBox textBox = (TextBox)host.CreateComponent(typeof(TextBox), "textBox1");
				textBox.Location = new Point(5, 5);
				textBox.TabIndex = 0;
				textBox.Size = new Size(110, 20);
				panel.Controls.Add(textBox);
				
				// Add an event handler to the panel to check that this code is generated
				// before the text box is initialized.
				EventDescriptorCollection events = TypeDescriptor.GetEvents(panel);
				EventDescriptor clickEvent = events.Find("Click", false);
				PropertyDescriptor clickEventProperty = eventBindingService.GetEventProperty(clickEvent);
				clickEventProperty.SetValue(panel, "Panel1Click");

				form.Controls.Add(panel);
				
				string indentString = "    ";
				PythonForm pythonForm = new PythonForm(indentString);
				generatedPythonCode = pythonForm.GenerateInitializeComponentMethod(form);
			}
		}
		
		[Test]
		public void GeneratedCode()
		{
			string expectedCode = "def InitializeComponent(self):\r\n" +
								"    self._panel1 = System.Windows.Forms.Panel()\r\n" +
								"    self._textBox1 = System.Windows.Forms.TextBox()\r\n" +
								"    self._panel1.SuspendLayout()\r\n" +
								"    self.SuspendLayout()\r\n" +
								"    # \r\n" +
								"    # panel1\r\n" +
								"    # \r\n" +
								"    self._panel1.Controls.Add(self._textBox1)\r\n" +
								"    self._panel1.Location = System.Drawing.Point(10, 15)\r\n" +
								"    self._panel1.Name = \"panel1\"\r\n" +
								"    self._panel1.Size = System.Drawing.Size(100, 120)\r\n" +
								"    self._panel1.TabIndex = 0\r\n" +
								"    self._panel1.Click += self.Panel1Click\r\n" +
								"    # \r\n" +
								"    # textBox1\r\n" +
								"    # \r\n" +
								"    self._textBox1.Location = System.Drawing.Point(5, 5)\r\n" +
								"    self._textBox1.Name = \"textBox1\"\r\n" +
								"    self._textBox1.Size = System.Drawing.Size(110, 20)\r\n" +
								"    self._textBox1.TabIndex = 0\r\n" +
								"    # \r\n" +
								"    # MainForm\r\n" +
								"    # \r\n" +
								"    self.ClientSize = System.Drawing.Size(284, 264)\r\n" +
								"    self.Controls.Add(self._panel1)\r\n" +
								"    self.Name = \"MainForm\"\r\n" +
								"    self._panel1.ResumeLayout(False)\r\n" +
								"    self._panel1.PerformLayout()\r\n" +
								"    self.ResumeLayout(False)\r\n" +
								"    self.PerformLayout()\r\n";
			
			Assert.AreEqual(expectedCode, generatedPythonCode);
		}
	}
}
