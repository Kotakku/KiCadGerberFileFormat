using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace KiCadGerberFileFormat
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void FolderBrowseButton_Click(object sender, EventArgs e)
        {
            //FolderBrowserDialogクラスのインスタンスを作成
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            //上部に表示する説明テキストを指定する
            fbd.Description = "フォルダを指定してください。";
            //ルートフォルダを指定する
            //デフォルトでDesktop
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            //最初に選択するフォルダを指定する
            //RootFolder以下にあるフォルダである必要がある
            fbd.SelectedPath = @"D:\Users\Owner\Documents\KiCad";
            //ユーザーが新しいフォルダを作成できるようにする
            //デフォルトでTrue
            fbd.ShowNewFolderButton = true;

            //ダイアログを表示する
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                //選択されたフォルダを表示する
                //Console.WriteLine(fbd.SelectedPath);
                fileNameTextBox.Text = fbd.SelectedPath;
            }
        }

        private void FormatButton_Click(object sender, EventArgs e)
        {
            string parentFolderName = @fileNameTextBox.Text;
            string folderName = @fileNameTextBox.Text + "\\exportZip_Log";

            if (Directory.Exists(parentFolderName))
            {
                //zip作成の時の名前に使う
                string baseFileName = "";

                logTextBox.Text = "";
                //指定フォルダの中にフォルダを作り中身をコピー、そっちで作業する
                logTextBox.AppendText("parent Folder: " + parentFolderName + "\r\n");
                logTextBox.AppendText("log    Folder: " + folderName + "\r\n\r\n");

                DirectoryCopy(parentFolderName, folderName, false);

                //フォルダにあるファイルの数を取得
                int fileCount = Directory.GetFiles(folderName, "*", SearchOption.TopDirectoryOnly).Length;

                if(fileCount > 0)
                {
                    logTextBox.AppendText("at '" + folderName + "'\r\n  ファイルは" + fileCount + "個です。\r\n\r\n");
                }
                else
                {
                    logTextBox.AppendText("ファイルが存在しません。\r\n");
                    return;
                }

                //全てのファイル名を取得
                string[] past_fileNames = new string[fileCount];
                string[] fileNames = new string[fileCount];

                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(folderName);

                IEnumerable<System.IO.FileInfo> files = di.EnumerateFiles("*", System.IO.SearchOption.AllDirectories);
                int i = 0;
                foreach (System.IO.FileInfo f in files)
                {
                    past_fileNames[i] = Path.GetFileName(f.FullName);
                    fileNames[i++] = Path.GetFileName(f.FullName);
                }


                //ファイル名の最低限の部分の長さを数える
                int baseFileNameLength = -1;
                for (i = 0; i < fileCount; i++)
                {
                    if(baseFileNameLength == -1)
                    {
                        baseFileNameLength = fileNames[i].LastIndexOf('.');
                    }
                    else
                    {
                        if(fileNames[i].LastIndexOf('.') < baseFileNameLength)
                        {
                            baseFileNameLength = fileNames[i].LastIndexOf('.');
                        }
                    }
                }
                baseFileName = fileNames[0].Substring(0, baseFileNameLength);
                //logTextBox.AppendText("\r\n");

                //各ファイルの変換後の名前を出す
                for (i = 0; i < fileCount; i++)
                {
                    // '-'より後を消す
                    logTextBox.AppendText(fileNames[i] + "\r\n");
                    int periodPosition = fileNames[i].LastIndexOf('.');
                    int hyphenPosition = fileNames[i].LastIndexOf('-');
                    
                    if(hyphenPosition >= baseFileNameLength)
                    {
                        fileNames[i] = fileNames[i].Remove(hyphenPosition, periodPosition - hyphenPosition);
                    }
                    logTextBox.AppendText("     ->  " + fileNames[i] + "\r\n");


                    //拡張子を変換する
                    //ドリルファイル(.drl)を(.txt)に変換
                    periodPosition = fileNames[i].LastIndexOf('.');
                    if (fileNames[i].Substring(baseFileNameLength + 1) == "drl")
                    {
                        fileNames[i] = fileNames[i].Remove(periodPosition + 1, 3);
                        fileNames[i] += "txt";
                        logTextBox.AppendText("    *->  " + fileNames[i] + "\r\n");
                    }
                    //基板外形データ(.gm1)を(.gko)に変換
                    if (fileNames[i].Substring(baseFileNameLength + 1) == "gm1")
                    {
                        fileNames[i] = fileNames[i].Remove(periodPosition + 1, 3);
                        fileNames[i] += "gko";
                        logTextBox.AppendText("    *->  " + fileNames[i] + "\r\n");
                    }
                    logTextBox.AppendText("\r\n");
                }

                //ファイル名を変換
                for (i = 0; i < fileCount; i++)
                {
                    try
                    {
                        if(past_fileNames[i] != fileNames[i])
                        {
                            File.Copy(folderName + "\\" + past_fileNames[i], folderName + "\\" + fileNames[i], true);
                            File.Delete(folderName + "\\" + past_fileNames[i]);
                        }
                    }
                    catch(Exception a)
                    {
                        logTextBox.AppendText(a + "\r\n");
                    }

                }

                //余分なファイルがあれば削除
                for(i = 0; i < fileCount; i++)
                {
                    int periodPosition = fileNames[i].LastIndexOf('.');
                    string fileExtension = fileNames[i].Substring(periodPosition + 1).ToLower();
                    string[] extensions = { "gbl", "gbo", "gbs", "gko", "gtl", "gto", "gts", "txt" };
                    bool isDelete = true;

                    //logTextBox.AppendText(fileExtension + "\r\n");
                    foreach (string ext in extensions)
                    {
                        if (fileExtension == ext)
                            isDelete = false;
                    }

                    if(isDelete)
                    {
                        //logTextBox.AppendText("DELETE:" + fileNames[i] + "\r\n");
                        File.Delete(folderName + "\\" + fileNames[i]);
                    }
                }

                //フォルダの中身を(.zip)にまとめる
                if (zipCheckBox.Checked == true)
                {
                    try
                    {
                        string zipPath = parentFolderName + "\\" + baseFileName + ".zip";

                        //同じ名前のzipファイルが存在する場合、一度消す
                        if(File.Exists(zipPath))
                        {
                            File.Delete(zipPath);
                        }
                        ZipFile.CreateFromDirectory(folderName, zipPath);
                        logTextBox.AppendText("\r\n" + baseFileName + ".zipを作成しました\r\n");
                    }
                    catch (Exception a)
                    {
                        logTextBox.AppendText(a + "\r\n");
                    }
                }
            }
            else
            {
                logTextBox.AppendText("'" + folderName + "'は存在しません。\r\n");
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private void fileNameTextBox_DragEnter(object sender, DragEventArgs e)
        {
            if( ((string[])e.Data.GetData(DataFormats.FileDrop, false)).Length == 1 )
                e.Effect = DragDropEffects.All;
        }

        private void fileNameTextBox_DragDrop(object sender, DragEventArgs e)
        {
            string[] fileName = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            fileNameTextBox.Text = fileName[0];
        }
    }
}
