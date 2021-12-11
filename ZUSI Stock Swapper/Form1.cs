using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace ZUSI_Stock_Swapper
{
    public partial class Form1 : Form
    {
        string tempPath = Path.GetTempPath(); //the PCs temporary file directory
        List<string> idHaupt = new List<string>();
        Dictionary<Control, string> deutsch = new Dictionary<Control, string>();
        Dictionary<Control, string> english = new Dictionary<Control, string>();
        public Form1()
        {
            InitializeComponent();
            initDictionary();
        }

        /// <summary>
        /// Open a file dialog pertaining to the consist being swapped
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "TRN File|*.trn";
            openFileDialog1.Title = "Open train";
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;

                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.Load(openFileDialog1.FileName);
                XmlNodeList elemList = doc.GetElementsByTagName("FahrzeugInfo"); //get all components of the consist

                listBox1.Items.Clear();

                foreach (XmlNode elem in elemList)
                {
                    listBox1.Items.Add(elem["Datei"].Attributes["Dateiname"].Value);
                }
            }
        }

        /// <summary>
        /// Open a file dialog pertaining to the item to swap to
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
            {
                if (button4.Text != "English") //current language is English
                {
                    MessageBox.Show("You haven't selected an item to swap yet.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                else
                {
                    MessageBox.Show("Sie haben noch keinen Gegenstand zum Tauschen ausgewählt.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            } else
            {
                string selected = listBox1.SelectedItem.ToString();
                string trainType = substringFromTo(selected, indexOfNth(selected, "\\", 2)+1, indexOfNth(selected, "\\", 3)); //get the type of fragment (e.g. Electroloks)

                openFileDialog1.Filter = "FZG File|*.fzg";
                openFileDialog1.Title = "Open fragment";
                DialogResult result = openFileDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.PreserveWhitespace = true;
                    doc.Load(openFileDialog1.FileName);
                    XmlNodeList elemList = doc.GetElementsByTagName("FahrzeugVariante"); //get all components of the consist

                    listBox2.Items.Clear();

                    foreach (XmlNode elem in elemList)
                    {
                        string replacement = elem["DateiAussenansicht"].Attributes["Dateiname"].Value;
                        string replaceType = substringFromTo(replacement, indexOfNth(replacement, "\\", 2) + 1, indexOfNth(replacement, "\\", 3)); //get the type of fragment (e.g. Electroloks)
                        if (!replaceType.Equals(trainType)) //train to be replaced and replacement train are not same type
                        {
                            if (button4.Text != "English") //current language is English
                            {
                                MessageBox.Show("Fragment types do not match.\nYour selected item is part of the " + trainType + " family but the replacement you selected is " +
                                "part of the " + replaceType + " family.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show("Die Fragmenttypen stimmen nicht überein.\nIhr ausgewähltes Element ist Teil des " + trainType + " Familie, aber der von Ihnen gewählte Ersatz ist " +
                                "teil der " + replaceType + " Familie.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            break;
                        } else
                        {
                            //list all variants of the loco
                            listBox2.Items.Add(elem.Attributes["Beschreibung"].Value + " (" + elem.Attributes["Farbgebung"].Value + ")");
                            idHaupt.Add(elem.Attributes["IDHaupt"].Value);
                            textBox2.Text = openFileDialog1.FileName;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Perform the swap
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex != -1)
            {
                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.Load(textBox1.Text); //load original train file
                XmlNodeList elemList = doc.GetElementsByTagName("FahrzeugInfo"); //get all components of the consist

                foreach (XmlNode elem in elemList)
                {
                    if (elem["Datei"].Attributes["Dateiname"].Value.Contains(listBox1.SelectedItem.ToString()))
                    {
                        //save the original folder in Temp (in case the new .trn file doesn't work and/or user wants to revert)
                        if (!Directory.Exists(tempPath + "ZSS"))
                        {
                            Directory.CreateDirectory(tempPath + "ZSS");
                        }
                        using (TextWriter sw = new StreamWriter(tempPath + "ZSS\\" + substringFromTo(textBox1.Text, textBox1.Text.LastIndexOf("\\") + 1, textBox1.Text.Length), false, new UTF8Encoding(false)))
                        {
                            doc.Save(sw);
                        }
                        elem["Datei"].Attributes["Dateiname"].Value = elem["Datei"].Attributes["Dateiname"].Value.Replace(listBox1.SelectedItem.ToString(),
                            substringFromTo(textBox2.Text, textBox2.Text.IndexOf("RollingStock"), textBox2.Text.Length));
                        elem.Attributes["IDHaupt"].Value = idHaupt[listBox2.SelectedIndex];
                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.Encoding = new UTF8Encoding(false); //false means do not emit the BOM - emitting it will make the .bin files completely empty
                        using (TextWriter sw = new StreamWriter(textBox1.Text, false, new UTF8Encoding(false)))
                        {
                            doc.Save(sw);
                        }
                        if (button4.Text != "English") //current language is English
                        {
                            MessageBox.Show("Item has been replaced.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Der Gegenstand wurde ersetzt.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            } else
            {
                if (button4.Text != "English") //current language is English
                {
                    MessageBox.Show("You haven't selected the variant of the item to swap yet.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                } else
                {
                    MessageBox.Show("Sie haben noch nicht die Variante des zu tauschenden Artikels ausgewählt.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                
            }
        }

        /// <summary>
        /// Finds the nth index of a substring in a string.
        /// </summary>
        /// <param name="str">The string to search.</param>
        /// <param name="value">The substring to look for.</param>
        /// <param name="nth">What occurence of the substring to find.</param>
        /// <returns>An integer corresponding to the position of the nth index of value in str</returns>
        public static int indexOfNth(string str, string value, int nth = 0)
        {
            if (nth < 0)
                throw new ArgumentException("Can not find a negative index of substring in string. Must start with 0");

            int offset = str.IndexOf(value);
            for (int i = 0; i < nth; i++)
            {
                if (offset == -1) return -1;
                offset = str.IndexOf(value, offset + 1);
            }

            return offset;
        }

        /// <summary>
        /// In complement with the above, finds substring between indices (if that's how the plural is spelt!)
        /// </summary>
        /// <param name="str">The string to search.</param>
        /// <param name="from">The starting index of the string to search</param>
        /// <param name="to">The ending index of the string, stop searching here</param>
        /// <returns>The substring of str from the start to end index specified by the code</returns>
        public string substringFromTo(string str, int from, int to)
        {
            try
            {
                if (str.Substring(from, to - from).Equals(""))
                {
                    return "None";
                }
                return str.Substring(from, to - from);
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private void initDictionary()
        {
            deutsch.Add(button1, "Offen");
            deutsch.Add(button2, "Offen");
            deutsch.Add(button3, "Tauschen Sie");
            deutsch.Add(button4, "English");

            english.Add(button1, "Open");
            english.Add(button2, "Open");
            english.Add(button3, "Swap");
            english.Add(button4, "Deutsch");

            deutsch.Add(label1, "Zu ändernden Zug auswählen");
            deutsch.Add(label2, "Zu ändernde Komponente auswählen");
            deutsch.Add(label3, "Tausch mit");
            deutsch.Add(label4, "Der zu tauschende Gegenstand muss vom gleichen Typ sein wie der Gegenstand, mit dem getauscht wird");
            deutsch.Add(label5, "Sie können zum Beispiel eine Diesellokomotive nicht mit einer Elektrolokomotive austauschen.");
            deutsch.Add(label6, "Variante auswählen");

            english.Add(label1, "Select train to modify:");
            english.Add(label2, "Select component to modify");
            english.Add(label3, "Swap with:");
            english.Add(label4, "The item you will swap to must be the same type as the item being swapped.");
            english.Add(label5, "i.e. You cannot swap a diesel locomotive with an electric locomotive.");
            english.Add(label6, "Select variant");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text.Equals("Deutsch"))
            {
                button4.Text = "English";
                foreach (KeyValuePair<Control, string> kvp in deutsch)
                {
                    kvp.Key.Text = kvp.Value;
                }
            } else
            {
                button4.Text = "Deutsch";
                foreach (KeyValuePair<Control, string> kvp in english)
                {
                    kvp.Key.Text = kvp.Value;
                }
            }
        }
    }
}
