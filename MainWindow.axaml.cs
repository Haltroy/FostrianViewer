using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LibFoster;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace FostrianViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow LoadWithArgs(string[]? args)
        {
            if (args != null && args.Length > 0 && args[0] is string fileName)
            {
                Task.Run(async () =>
                {
                    CurrentRoot = Fostrian.Parse(fileName);
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { LoadFostrian(CurrentRoot, null, true); }, Avalonia.Threading.DispatcherPriority.Render);
                    filePath = await StorageProvider.TryGetFileFromPath(fileName);
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { Title = $"{System.IO.Path.GetFileName(fileName)} - Fostrian Viewer"; }, Avalonia.Threading.DispatcherPriority.Render);
                });
            }
            return this;
        }

        private Fostrian.FostrianNode? CurrentRoot;
        private IStorageFile? filePath;

        private async void Open_Clicked(object? s, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                if (!StorageProvider.CanOpen) { return; }
                var files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions()
                {
                    AllowMultiple = false,
                    Title = "Open a Fostrian file...",
                    FileTypeFilter = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("Fostrian file") { Patterns = new string[] { "*.fostrian", "*.fff", "*.fvf" } },
                       FilePickerFileTypes.All
                    }
                });

                if (files != null && files.Count > 0 && files[0] is IStorageFile fileName)
                {
                    var str = await fileName.OpenReadAsync();
                    if (str.CanRead)
                    {
                        CurrentRoot = Fostrian.Parse(str);
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            LoadFostrian(CurrentRoot, null, true);
                        }, Avalonia.Threading.DispatcherPriority.Render);
                    }
                    str.Close();
                    filePath = fileName;
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { Title = $"{fileName.Name} - Fostrian Viewer"; }, Avalonia.Threading.DispatcherPriority.Render);
                }
            });
        }

        private async void OpenCustom_Clicked(object? s, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                if (!StorageProvider.CanOpen) { return; }
                var files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions()
                {
                    AllowMultiple = false,
                    Title = "Open a Fostrian file...",
                    FileTypeFilter = new List<FilePickerFileType>() { FilePickerFileTypes.All }
                });

                if (files != null && files.Count > 0 && files[0] is IStorageFile fileName)
                {
                    System.IO.Stream str = await fileName.OpenReadAsync();

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        var start_end = await new CustomWindow().ShowDialog<long[]>(this);

                        if (str.CanRead && start_end != null && start_end[0] is long start && start_end[1] is long end)
                        {
                            if (str.CanSeek) { str.Position = start; }
                            CurrentRoot = Fostrian.Parse(str, end);
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                LoadFostrian(CurrentRoot, null, true);
                            }, Avalonia.Threading.DispatcherPriority.Render);
                            str.Close();
                            filePath = fileName;
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { Title = $"[custom] {fileName.Name} - Fostrian Viewer"; }, Avalonia.Threading.DispatcherPriority.Render);
                        }
                    }, Avalonia.Threading.DispatcherPriority.Input);
                }
            });
        }

        private async void Save_Clicked(object? s, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                if (filePath != null && filePath.CanOpenWrite)
                {
                    var str = await filePath.OpenWriteAsync();
                    if (str.CanWrite)
                    {
                        CurrentRoot.Recreate(str);
                    }
                    str.Close();
                }
                else
                {
                    SaveAs_Clicked(s, e);
                }
            });
        }

        private async void SaveAs_Clicked(object? s, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                if (!StorageProvider.CanSave) { return; }
                var saveFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
                {
                    Title = "Save Fostrian file to...",
                    DefaultExtension = ".fostrian",
                    ShowOverwritePrompt = true,
                    FileTypeChoices = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("Fostrian file") { Patterns = new string[] { "*.fostrian", "*.fff", "*.fvf" } },
                       FilePickerFileTypes.All
                    }
                });

                if (saveFile != null && saveFile.CanOpenWrite)
                {
                    filePath = saveFile;
                    var str = await filePath.OpenWriteAsync();
                    if (str.CanWrite)
                    {
                        CurrentRoot.Recreate(str);
                    }
                    str.Close();
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { Title = $"{saveFile.Name} - Fostrian Viewer"; }, Avalonia.Threading.DispatcherPriority.Render);
                }
            });
        }

        private async void ImportXML_Clicked(object? s, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                if (!StorageProvider.CanOpen) { return; }
                var files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions()
                {
                    AllowMultiple = false,
                    Title = "Open a XML file...",
                    FileTypeFilter = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("XML file") { Patterns = new string[] { "*.xml"} },
                       FilePickerFileTypes.All
                    }
                });

                if (files != null && files.Count > 0 && files[0] is IStorageFile fileName)
                {
                    var str = await fileName.OpenReadAsync();
                    if (str.CanRead)
                    {
                        var doc = new XmlDocument();
                        doc.Load(str);
                        if (doc.DocumentElement is null) { return; }
                        CurrentRoot = Fostrian.GenerateRootNode();
                        CurrentRoot.Encoding = (System.Text.Encoding?)Fostrian.GetFostrianEncoding((byte)cbEncoding.SelectedIndex);
                        CurrentRoot.StartByte = 0x02;
                        CurrentRoot.EndByte = 0x03;
                        XmlNodeToFostrian(doc.DocumentElement, CurrentRoot);
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { LoadFostrian(CurrentRoot, null, true); }, Avalonia.Threading.DispatcherPriority.Render);
                    }
                    str.Close();
                    filePath = fileName;
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { Title = $"fileName.Name}} - Fostrian Viewer"; }, Avalonia.Threading.DispatcherPriority.Render);
                }
            });
        }

        private async void ExportXML_Clicked(object? s, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                if (!StorageProvider.CanSave) { return; }
                var saveFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
                {
                    Title = "Save XML file to...",
                    DefaultExtension = ".xml",
                    ShowOverwritePrompt = true,
                    FileTypeChoices = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("XML file") { Patterns = new string[] { "*.xml"} },
                       FilePickerFileTypes.All
                    }
                });

                if (saveFile != null && saveFile.CanOpenWrite)
                {
                    var str = await saveFile.OpenWriteAsync();
                    if (str.CanWrite && CurrentRoot != null)
                    {
                        using System.IO.StreamWriter writer = new(str, Fostrian.GetFostrianEncoding((byte)cbEncoding.SelectedIndex));
                        writer.WriteLine(FostrianToXML(CurrentRoot));
                    }
                    str.Close();
                }
            });
        }

        public Fostrian.FostrianNode XmlNodeToFostrian(System.Xml.XmlNode node, Fostrian.FostrianNode root)
        {
            var fnode = root.Add((object)node.InnerText);
            fnode.Name = node.Name;

            if (node.Attributes is XmlAttributeCollection attrs && attrs.Count > 0)
            {
                for (int i = 0; i < attrs.Count; i++)
                {
                    var attr = attrs[i];
                    switch (attr.Name.ToLowerInvariant())
                    {
                        case "name":
                            fnode.Name = attr.Value;
                            break;

                        case "value":
                            fnode.Data = ConvertToApprFostrianDataType(attr.Value, null);
                            break;

                        default:
                            {
                                var fattr = fnode.Add(attr.Value);
                                fattr.Name = attr.Name;
                                break;
                            }
                    }
                }
            }

            if (node.ChildNodes.Count > 0)
            {
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    if (node.ChildNodes[i] is XmlNode cnode)
                        XmlNodeToFostrian(cnode, fnode);
                }
            }

            return fnode;
        }

        public void LoadFostrian(Fostrian.FostrianNode node, TreeViewItem? parent, bool clear = false)
        {
            TreeViewItem item = new() { Tag = node, Header = (string.IsNullOrWhiteSpace(node.Name) ? "" : node.Name + ": ") + node.Data };
            if (node.Size > 0)
            {
                for (int i = 0; i < node.Size; i++)
                {
                    LoadFostrian(node[i], item);
                }
            }
            if (parent is TreeViewItem p_item)
            {
                if (p_item.Items is AvaloniaList<object> list)
                {
                    list.Add(item);
                }
            }
            else
            {
                CurrentRoot = node;
                if (FostrianDatas.Items is AvaloniaList<object> list)
                {
                    if (clear) { list.Clear(); }
                    list.Add(item);
                }
            }
        }

        public string FostrianToXML(Fostrian.FostrianNode node, int level = 0)
        {
            string xml = (node.IsRoot ? "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + "<root>" : "") + Environment.NewLine;

            string spaces = "";

            for (int i = 0; i < level * 3; i++) { spaces += " "; }

            xml += $"{spaces}<Node {(node.Type == Fostrian.NodeType.FVF ? "Name=\"" + node.Name + "\" " : "")} Value=\"{node.Data}\" {(node.Size <= 0 ? "/>" : ">")}{Environment.NewLine}";

            for (int i = 0; i < node.Size; i++)
            {
                xml += FostrianToXML(node[i], level + 1);
            }

            xml += $"{(node.Size > 0 ? "</Node>" + Environment.NewLine : "")}";

            return (node.IsRoot ? xml + Environment.NewLine + "</root>" : xml);
        }

        private void About_Clicked(object? s, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog(this);
        }

        private void Plus_Clicked(object? s, RoutedEventArgs e)
        {
            if (CurrentRoot is null)
            {
                if (FostrianDatas.Items is AvaloniaList<object> list)
                {
                    object? newVal = GetValue();
                    var newNode = Fostrian.GenerateRootNode(newVal);
                    CurrentRoot = newNode;
                    CurrentRoot.Encoding = (System.Text.Encoding?)Fostrian.GetFostrianEncoding((byte)cbEncoding.SelectedIndex);
                    CurrentRoot.StartByte = 0x02;
                    CurrentRoot.EndByte = 0x03;
                    if (!string.IsNullOrWhiteSpace(tbName.Text)) { newNode.Name = tbName.Text; }
                    var newTItem = new TreeViewItem()
                    {
                        Header = (string.IsNullOrWhiteSpace(newNode.Name) ? "" : newNode.Name + ": ") + newNode.Data,
                        Tag = newNode
                    };

                    list.Add(newTItem);
                    FostrianDatas.SelectedItem = newTItem;
                }
            }
            else if (FostrianDatas.SelectedItem is TreeViewItem item && item.Items is AvaloniaList<object> list && item.Tag is Fostrian.FostrianNode node)
            {
                object? newVal = GetValue();

                var newNode = node.Add(newVal);
                if (!string.IsNullOrWhiteSpace(tbName.Text)) { newNode.Name = tbName.Text; }

                var newTItem = new TreeViewItem()
                {
                    Header = (string.IsNullOrWhiteSpace(newNode.Name) ? "" : newNode.Name + ": ") + newNode.Data,
                    Tag = newNode
                };
                list.Add(newTItem);
                FostrianDatas.SelectedItem = newTItem;
            }
        }

        private object? GetValue()
        {
            object? newVal;
            switch (cbType.SelectedIndex)
            {
                case 0:
                    var bval = tbString.Text != null && (tbString.Text.ToLowerInvariant() == "true" || tbString.Text.ToLowerInvariant() == "yes" || tbString.Text.ToLowerInvariant() == "1");
                    newVal = bval;
                    break;

                case 1:
                    char cvar = tbString.Text != null ? tbString.Text[0] : ' ';
                    newVal = cvar;
                    break;

                case 10:
                    var strval = tbString.Text ?? string.Empty;
                    newVal = strval;
                    break;

                case 2:
                    short sval = (short)(tbString.Text is string sh ? (short.TryParse(sh, out short result) ? result : 0) : 0);
                    newVal = sval;
                    break;

                case 3:
                    ushort usval = (ushort)(tbString.Text is string us ? (ushort.TryParse(us, out ushort usresult) ? usresult : 0) : 0);
                    newVal = usval;
                    break;

                case 4:
                    int ival = tbString.Text is string i ? (int.TryParse(i, out int iresult) ? iresult : 0) : 0;
                    newVal = ival;
                    break;

                case 5:
                    uint uival = (uint)(tbString.Text is string ui ? (uint.TryParse(ui, out uint uiresult) ? uiresult : 0) : 0);
                    newVal = uival;
                    break;

                case 6:
                    long lval = tbString.Text is string l ? (long.TryParse(l, out long lresult) ? lresult : 0) : 0;
                    newVal = lval;

                    break;

                case 7:
                    ulong ulvar = tbString.Text is string ul ? (ulong.TryParse(ul, out ulong ulresult) ? ulresult : 0) : 0;
                    newVal = ulvar;
                    break;

                case 8:
                    float fvar = tbString.Text is string f ? (float.TryParse(f, out float fresult) ? fresult : 0) : 0;
                    newVal = fvar;
                    break;

                case 9:
                    double dvar = tbString.Text is string d ? (double.TryParse(d, out double dresult) ? dresult : 0) : 0;
                    newVal = dvar;
                    break;

                default:
                    throw new NotImplementedException("How???");
            }

            return newVal;
        }

        private void Minus_Clicked(object? s, RoutedEventArgs e)
        {
            if (FostrianDatas.SelectedItem is TreeViewItem item && item.Tag is Fostrian.FostrianNode node)
            {
                if (item.Parent == FostrianDatas && FostrianDatas.Items is AvaloniaList<object> list)
                {
                    list.Remove(item);
                    CurrentRoot = null;
                }

                if (item.Parent is TreeViewItem parent && parent.Items is AvaloniaList<object> list2)
                {
                    list2.Remove(item);
                    node.Parent.Remove(node);
                }
            }
        }

        private void SetCBType(object data)
        {
            var cb = data switch
            {
                char _ => 1,
                short _ => 2,
                ushort _ => 3,
                int _ => 4,
                uint _ => 5,
                long _ => 6,
                ulong _ => 7,
                float _ => 8,
                double _ => 9,
                string _ => 10,
                _ => 0,
            };
            cbType.SelectedIndex = cb;
        }

        private void Encoding_Changed(object? s, SelectionChangedEventArgs e)
        {
            if (CurrentRoot != null)
            {
                CurrentRoot.Encoding = (System.Text.Encoding?)Fostrian.GetFostrianEncoding((byte)cbEncoding.SelectedIndex);
                CurrentRoot.StartByte = 0x02;
                CurrentRoot.EndByte = 0x03;
            }
        }

        private void TreeView_Changed(object? s, SelectionChangedEventArgs e)
        {
            if (FostrianDatas.SelectedItem is TreeViewItem item && item.Tag is Fostrian.FostrianNode node)
            {
                tbName.Text = node.Name;
                SetCBType(node.Type);
                tbString.Text = "" + node.Data;
            }
        }

        private void Name_Changed(object? s, TextChangedEventArgs e)
        {
            if (FostrianDatas.SelectedItem is TreeViewItem item && item.Tag is Fostrian.FostrianNode node)
            {
                node.Name = tbName.Text;
                node.Type = string.IsNullOrWhiteSpace(tbName.Text) ? Fostrian.NodeType.FFF : Fostrian.NodeType.FVF;
                item.Header = (string.IsNullOrWhiteSpace(node.Name) ? "" : node.Name + ": ") + node.Data;
            }
        }

        private void Type_Changed(object? s, SelectionChangedEventArgs e)
        {
            if (FostrianDatas is null) return;
            object? newVal = GetValue();
            if (FostrianDatas.SelectedItem is TreeViewItem item && item.Tag is Fostrian.FostrianNode node)
            {
                node.Data = newVal;
                tbString.Text = node.Data + "";
                item.Header = (string.IsNullOrWhiteSpace(node.Name) ? "" : node.Name + ": ") + newVal;
            }
        }

        private void String_Changed(object? s, TextChangedEventArgs e)
        {
            if (FostrianDatas.SelectedItem is TreeViewItem item && item.Tag is Fostrian.FostrianNode node)
            {
                node.Data = string.IsNullOrWhiteSpace(tbString.Text) ? false : (object?)ConvertToApprFostrianDataType(tbString.Text, LockType.IsChecked is bool _lock && _lock ? node.Data : null);
                SetCBType(node.Type);
                item.Header = (string.IsNullOrWhiteSpace(node.Name) ? "" : node.Name + ": ") + node.Data;
            }
        }

        private static object ConvertToApprFostrianDataType(string input, object? lockObject)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                if (Regex.IsMatch(input, "^(\\-|\\+)?\\d+(\\.|\\,)?\\d+$"))
                {
                    if (lockObject != null)
                    {
                        switch (lockObject)
                        {
                            case short _:
                                short s;
                                return short.TryParse(input, out s) ? s : (short)0;

                            case ushort _:
                                ushort us;
                                return ushort.TryParse(input, out us) ? us : (ushort)0;

                            case int _:
                                int i;
                                return int.TryParse(input, out i) ? i : (int)0;

                            case uint _:
                                uint u;
                                return uint.TryParse(input, out u) ? u : (uint)0;

                            case long _:
                                long l;
                                return long.TryParse(input, out l) ? l : (long)0;

                            case ulong _:
                                ulong ul;
                                return ulong.TryParse(input, out ul) ? ul : (ulong)0;

                            case float _:
                                float f;
                                return float.TryParse(input, out f) ? f : (float)0;

                            case double _:
                                double d;
                                return double.TryParse(input, out d) ? d : (double)0;

                            default:
                                return false;
                        }
                    }
                    if (decimal.TryParse(input, out decimal number))
                    {
                        var floatMax = float.MaxValue; var floatMin = float.MinValue;
                        var doubleMax = double.MaxValue; var doubleMin = double.MinValue;
                        if (number > ushort.MinValue && number < ushort.MaxValue)
                        {
                            return (ushort)number;
                        }
                        if (number > short.MinValue && number < short.MaxValue)
                        {
                            return (short)number;
                        }
                        if (number > uint.MinValue && number < uint.MaxValue)
                        {
                            return (uint)number;
                        }
                        if (number > int.MinValue && number < int.MaxValue)
                        {
                            return (int)number;
                        }
                        if (number > ulong.MinValue && number < ulong.MaxValue)
                        {
                            return (ulong)number;
                        }
                        if (number > long.MinValue && number < long.MaxValue)
                        {
                            return (long)number;
                        }
                        if (number > (decimal)floatMin && number < (decimal)floatMax)
                        {
                            return (float)number;
                        }
                        if (number > (decimal)doubleMin && number < (decimal)doubleMax)
                        {
                            return (double)number;
                        }
                    }
                    return false;
                }
                else // string - char
                {
                    if (input.ToLowerInvariant() == "true" || input.ToLowerInvariant() == "yes" || input.ToLowerInvariant() == "t" || input.ToLowerInvariant() == "y" && lockObject is null)
                    {
                        return true;
                    }
                    if (input.ToLowerInvariant() == "false" || input.ToLowerInvariant() == "no" || input.ToLowerInvariant() == "f" || input.ToLowerInvariant() == "n" && lockObject is null)
                    {
                        return false;
                    }
                    return input.Length switch
                    {
                        > 1 => lockObject is null ? input : (lockObject is string s ? s : (lockObject is bool b ? b : ' ')),
                        1 => lockObject is null ? input[0] : (lockObject is string s2 ? s2 : (lockObject is bool b2 ? b2 : ' ')),
                        _ => false,
                    };
                }
            }
            else
            {
                return false;
            }
        }
    }
}