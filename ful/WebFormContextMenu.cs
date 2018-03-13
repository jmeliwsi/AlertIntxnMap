using System;
using System.Windows.Forms;

namespace FUL
{
	public class WebFormContextMenu
	{
		private WebBrowser browser;
		public ContextMenuStrip Menu;
		private ToolStripMenuItem cut = new ToolStripMenuItem("Cut");
		private ToolStripMenuItem copy = new ToolStripMenuItem("Copy");
		private ToolStripMenuItem paste = new ToolStripMenuItem("Paste");
		private ToolStripMenuItem selectAll = new ToolStripMenuItem("Select All");
		private ToolStripMenuItem print = new ToolStripMenuItem("Print...");
		private ToolStripMenuItem printPreview = new ToolStripMenuItem("Print Preview...");

		public string ClickedElement = string.Empty;

		public WebFormContextMenu(ref WebBrowser b)
		{
			browser = b;
			Menu = new ContextMenuStrip();
			Menu.Opening += Menu_Opening;

			cut.Click += ClickHandler;
			Menu.Items.Add(cut);

			copy.Click += ClickHandler;
			Menu.Items.Add(copy);

			paste.Click += ClickHandler;
			Menu.Items.Add(paste);

			selectAll.Click += ClickHandler;
			Menu.Items.Add(selectAll);

			print.Click += ClickHandler;
			Menu.Items.Add(print);

			printPreview.Click += ClickHandler;
			Menu.Items.Add(printPreview);
		}

		private void Menu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (browser == null || browser.Document == null)
			{
				e.Cancel = true;
				return;
			}

			mshtml.IHTMLDocument2 doc = browser.Document.DomDocument as mshtml.IHTMLDocument2;

			cut.Enabled = (doc == null || doc.queryCommandEnabled("Cut"));
			copy.Enabled = (doc == null || doc.queryCommandEnabled("Copy"));
			paste.Enabled = (doc == null || doc.queryCommandEnabled("Paste"));
			selectAll.Enabled = (doc == null || doc.queryCommandEnabled("SelectAll"));
		}

		private void ClickHandler(object sender, EventArgs e)
		{
			switch ((sender as ToolStripMenuItem).Text)
			{
				case "Cut":
					browser.Document.ExecCommand("Cut", false, null);
					break;
				case "Copy":
					browser.Document.ExecCommand("Copy", false, null);
					break;
				case "Paste":
					browser.Document.ExecCommand("Paste", false, null);
					break;
				case "Select All":
					if (string.IsNullOrEmpty(ClickedElement) || ClickedElement.StartsWith("TabbedPanels"))
						browser.Document.ExecCommand("SelectAll", false, null);
					else
						browser.Document.InvokeScript("SelectText", new object[] { ClickedElement });
					break;
				case "Print...":
					browser.Document.ExecCommand("Print", false, null);
					break;
				case "Print Preview...":
					browser.ShowPrintPreviewDialog();
					break;
			}
		}
	}
}
