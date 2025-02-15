﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright © 2007-2015 ShareX Developers

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using ShareX.ImageEffectsLib;
using ShareX.Properties;
using ShareX.ScreenCaptureLib;
using ShareX.UploadersLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ShareX
{
    public partial class TaskSettingsForm : Form
    {
        public TaskSettings TaskSettings { get; private set; }
        public bool IsDefault { get; private set; }

        private ToolStripDropDownItem tsmiImageFileUploaders, tsmiTextFileUploaders;
        private bool loaded;

        private readonly string ConfigureEncoder = Resources.TaskSettingsForm_ConfigureEncoder_Configure_CLI_video_encoders_____;

        public TaskSettingsForm(TaskSettings hotkeySetting, bool isDefault = false)
        {
            InitializeComponent();
            Icon = ShareXResources.Icon;
            TaskSettings = hotkeySetting;
            IsDefault = isDefault;

            if (IsDefault)
            {
                tcTaskSettings.TabPages.Remove(tpTask);
                chkUseDefaultGeneralSettings.Visible = chkUseDefaultImageSettings.Visible = chkUseDefaultCaptureSettings.Visible = chkUseDefaultActions.Visible =
                    chkUseDefaultUploadSettings.Visible = chkUseDefaultIndexerSettings.Visible = chkUseDefaultAdvancedSettings.Visible = false;
            }
            else
            {
                tbDescription.Text = TaskSettings.Description ?? string.Empty;
                cbUseDefaultAfterCaptureSettings.Checked = TaskSettings.UseDefaultAfterCaptureJob;
                cbUseDefaultAfterUploadSettings.Checked = TaskSettings.UseDefaultAfterUploadJob;
                cbUseDefaultDestinationSettings.Checked = TaskSettings.UseDefaultDestinations;
                chkUseDefaultGeneralSettings.Checked = TaskSettings.UseDefaultGeneralSettings;
                chkUseDefaultImageSettings.Checked = TaskSettings.UseDefaultImageSettings;
                chkUseDefaultCaptureSettings.Checked = TaskSettings.UseDefaultCaptureSettings;
                chkUseDefaultActions.Checked = TaskSettings.UseDefaultActions;
                chkUseDefaultUploadSettings.Checked = TaskSettings.UseDefaultUploadSettings;
                chkUseDefaultIndexerSettings.Checked = TaskSettings.UseDefaultIndexerSettings;
                chkUseDefaultAdvancedSettings.Checked = TaskSettings.UseDefaultAdvancedSettings;
            }

            UpdateWindowTitle();

            AddEnumItemsContextMenu<HotkeyType>(x =>
            {
                TaskSettings.Job = x;
                UpdateWindowTitle();
            }, cmsTask);
            AddMultiEnumItemsContextMenu<AfterCaptureTasks>(x => TaskSettings.AfterCaptureJob = TaskSettings.AfterCaptureJob.Swap(x), cmsAfterCapture);
            AddMultiEnumItemsContextMenu<AfterUploadTasks>(x => TaskSettings.AfterUploadJob = TaskSettings.AfterUploadJob.Swap(x), cmsAfterUpload);
            // Destinations -> Image uploader
            AddEnumItems<ImageDestination>(x =>
            {
                TaskSettings.ImageDestination = x;
                // if click on "folder" with file destinations then set ImageFileDestination and check it
                if (x == ImageDestination.FileUploader)
                {
                    SetEnumChecked(TaskSettings.ImageFileDestination, tsmiImageFileUploaders);
                }
                else // if click not on "folder" with destinations then uncheck file destinations
                {
                    MainForm.Uncheck(tsmiImageFileUploaders);
                }
            }, tsmiImageUploaders);
            tsmiImageFileUploaders = (ToolStripDropDownItem)tsmiImageUploaders.DropDownItems[tsmiImageUploaders.DropDownItems.Count - 1];
            AddEnumItems<FileDestination>(x =>
            {
                TaskSettings.ImageFileDestination = x;
                tsmiImageFileUploaders.PerformClick();
            }, tsmiImageFileUploaders);
            // Destinations -> Text uploader
            AddEnumItems<TextDestination>(x =>
            {
                TaskSettings.TextDestination = x;
                // if click on "folder" with file destinations then set TextFileDestination and check it
                if (x == TextDestination.FileUploader)
                {
                    SetEnumChecked(TaskSettings.TextFileDestination, tsmiTextFileUploaders);
                }
                else // if click not on "folder" with destinations then uncheck file destinations
                {
                    MainForm.Uncheck(tsmiTextFileUploaders);
                }
            }, tsmiTextUploaders);
            tsmiTextFileUploaders = (ToolStripDropDownItem)tsmiTextUploaders.DropDownItems[tsmiTextUploaders.DropDownItems.Count - 1];
            AddEnumItems<FileDestination>(x =>
            {
                TaskSettings.TextFileDestination = x;
                tsmiTextFileUploaders.PerformClick();
            }, tsmiTextFileUploaders);
            // Destinations -> File uploader
            AddEnumItems<FileDestination>(x => TaskSettings.FileDestination = x, tsmiFileUploaders);
            AddEnumItems<UrlShortenerType>(x => TaskSettings.URLShortenerDestination = x, tsmiURLShorteners);
            AddEnumItems<URLSharingServices>(x => TaskSettings.URLSharingServiceDestination = x, tsmiURLSharingServices);

            SetEnumCheckedContextMenu(TaskSettings.Job, cmsTask);
            SetMultiEnumCheckedContextMenu(TaskSettings.AfterCaptureJob, cmsAfterCapture);
            SetMultiEnumCheckedContextMenu(TaskSettings.AfterUploadJob, cmsAfterUpload);
            SetEnumChecked(TaskSettings.ImageDestination, tsmiImageUploaders);
            MainForm.SetImageFileDestinationChecked(TaskSettings.ImageDestination, TaskSettings.ImageFileDestination, tsmiImageFileUploaders);
            SetEnumChecked(TaskSettings.TextDestination, tsmiTextUploaders);
            MainForm.SetTextFileDestinationChecked(TaskSettings.TextDestination, TaskSettings.TextFileDestination, tsmiTextFileUploaders);
            SetEnumChecked(TaskSettings.FileDestination, tsmiFileUploaders);
            SetEnumChecked(TaskSettings.URLShortenerDestination, tsmiURLShorteners);
            SetEnumChecked(TaskSettings.URLSharingServiceDestination, tsmiURLSharingServices);

            if (Program.UploadersConfig != null)
            {
                // FTP
                if (Program.UploadersConfig.FTPAccountList.Count > 0)
                {
                    chkOverrideFTP.Checked = TaskSettings.OverrideFTP;
                    cboFTPaccounts.Items.Clear();
                    cboFTPaccounts.Items.AddRange(Program.UploadersConfig.FTPAccountList.ToArray());
                    cboFTPaccounts.SelectedIndex = TaskSettings.FTPIndex.BetweenOrDefault(0, Program.UploadersConfig.FTPAccountList.Count - 1);
                }

                // Custom uploader
                if (Program.UploadersConfig.CustomUploadersList.Count > 0)
                {
                    chkOverrideCustomUploader.Checked = TaskSettings.OverrideCustomUploader;
                    cbOverrideCustomUploader.Items.Clear();
                    cbOverrideCustomUploader.Items.AddRange(Program.UploadersConfig.CustomUploadersList.ToArray());
                    cbOverrideCustomUploader.SelectedIndex = TaskSettings.CustomUploaderIndex.BetweenOrDefault(0, Program.UploadersConfig.CustomUploadersList.Count - 1);
                }
            }

            UpdateDestinationStates();
            UpdateUploaderMenuNames();

            // General
            cbPlaySoundAfterCapture.Checked = TaskSettings.GeneralSettings.PlaySoundAfterCapture;
            cbShowAfterCaptureTasksForm.Checked = TaskSettings.GeneralSettings.ShowAfterCaptureTasksForm;
            chkShowBeforeUploadForm.Checked = TaskSettings.GeneralSettings.ShowBeforeUploadForm;
            cbPlaySoundAfterUpload.Checked = TaskSettings.GeneralSettings.PlaySoundAfterUpload;
            chkShowAfterUploadForm.Checked = TaskSettings.GeneralSettings.ShowAfterUploadForm;
            cboPopUpNotification.Items.Clear();
            cboPopUpNotification.Items.AddRange(Helpers.GetLocalizedEnumDescriptions<PopUpNotificationType>());
            cboPopUpNotification.SelectedIndex = (int)TaskSettings.GeneralSettings.PopUpNotification;
            cbHistorySave.Checked = TaskSettings.GeneralSettings.SaveHistory;

            // Image - General
            cbImageFormat.Items.AddRange(Enum.GetNames(typeof(EImageFormat)));
            cbImageFormat.SelectedIndex = (int)TaskSettings.ImageSettings.ImageFormat;
            nudImageJPEGQuality.Value = TaskSettings.ImageSettings.ImageJPEGQuality;
            cbImageGIFQuality.Items.AddRange(Helpers.GetLocalizedEnumDescriptions<GIFQuality>());
            cbImageGIFQuality.SelectedIndex = (int)TaskSettings.ImageSettings.ImageGIFQuality;
            nudUseImageFormat2After.Value = TaskSettings.ImageSettings.ImageSizeLimit;
            cbImageFormat2.Items.AddRange(Enum.GetNames(typeof(EImageFormat)));
            cbImageFormat2.SelectedIndex = (int)TaskSettings.ImageSettings.ImageFormat2;
            cbImageFileExist.Items.Clear();
            cbImageFileExist.Items.AddRange(Helpers.GetLocalizedEnumDescriptions<FileExistAction>());
            cbImageFileExist.SelectedIndex = (int)TaskSettings.ImageSettings.FileExistAction;

            // Image - Effects
            chkShowImageEffectsWindowAfterCapture.Checked = TaskSettings.ImageSettings.ShowImageEffectsWindowAfterCapture;
            cbImageEffectOnlyRegionCapture.Checked = TaskSettings.ImageSettings.ImageEffectOnlyRegionCapture;

            // Image - Thumbnail
            nudThumbnailWidth.Value = TaskSettings.ImageSettings.ThumbnailWidth;
            nudThumbnailHeight.Value = TaskSettings.ImageSettings.ThumbnailHeight;
            txtThumbnailName.Text = TaskSettings.ImageSettings.ThumbnailName;
            lblThumbnailNamePreview.Text = "ImageName" + TaskSettings.ImageSettings.ThumbnailName + ".jpg";
            cbThumbnailIfSmaller.Checked = TaskSettings.ImageSettings.ThumbnailCheckSize;

            // Capture
            cbShowCursor.Checked = TaskSettings.CaptureSettings.ShowCursor;
            cbCaptureTransparent.Checked = TaskSettings.CaptureSettings.CaptureTransparent;
            cbCaptureShadow.Enabled = TaskSettings.CaptureSettings.CaptureTransparent;
            cbCaptureShadow.Checked = TaskSettings.CaptureSettings.CaptureShadow;
            nudCaptureShadowOffset.Value = TaskSettings.CaptureSettings.CaptureShadowOffset;
            cbCaptureClientArea.Checked = TaskSettings.CaptureSettings.CaptureClientArea;
            cbScreenshotDelay.Checked = TaskSettings.CaptureSettings.IsDelayScreenshot;
            nudScreenshotDelay.Value = TaskSettings.CaptureSettings.DelayScreenshot;
            cbCaptureAutoHideTaskbar.Checked = TaskSettings.CaptureSettings.CaptureAutoHideTaskbar;
            cboCaptureCustomRegionMonitors.Items.Clear();
            cboCaptureCustomRegionMonitors.Items.AddRange(MonitorRegionDefaultCreator.AllMonitorsRegions);
            cboCaptureCustomRegionMonitors.SelectedIndex = 0;
            nudCaptureCustomRegionX.Value = TaskSettings.CaptureSettings.CaptureCustomRegion.X;
            nudCaptureCustomRegionY.Value = TaskSettings.CaptureSettings.CaptureCustomRegion.Y;
            nudCaptureCustomRegionWidth.Value = TaskSettings.CaptureSettings.CaptureCustomRegion.Width;
            nudCaptureCustomRegionHeight.Value = TaskSettings.CaptureSettings.CaptureCustomRegion.Height;

            // Capture / Region capture
            if (TaskSettings.CaptureSettings.SurfaceOptions == null) TaskSettings.CaptureSettings.SurfaceOptions = new SurfaceOptions();
            pgRegionCapture.SelectedObject = TaskSettings.CaptureSettings.SurfaceOptions;

            // Capture / Rectangle annotate
            if (TaskSettings.CaptureSettings.RectangleAnnotateOptions == null) TaskSettings.CaptureSettings.RectangleAnnotateOptions = new RectangleAnnotateOptions();
            pgRectangleAnnotate.SelectedObject = TaskSettings.CaptureSettings.RectangleAnnotateOptions;

            // Capture / Screen recorder
            nudScreenRecordFPS.Value = TaskSettings.CaptureSettings.ScreenRecordFPS.Between((int)nudScreenRecordFPS.Minimum, (int)nudScreenRecordFPS.Maximum);
            nudGIFFPS.Value = TaskSettings.CaptureSettings.GIFFPS.Between((int)nudGIFFPS.Minimum, (int)nudGIFFPS.Maximum);
            cbGIFEncoding.Items.AddRange(Helpers.GetLocalizedEnumDescriptions<ScreenRecordGIFEncoding>());
            cbGIFEncoding.SelectedIndex = (int)TaskSettings.CaptureSettings.GIFEncoding;
            cbScreenRecorderFixedDuration.Checked = nudScreenRecorderDuration.Enabled = TaskSettings.CaptureSettings.ScreenRecordFixedDuration;
            nudScreenRecorderDuration.Value = (decimal)TaskSettings.CaptureSettings.ScreenRecordDuration;
            chkScreenRecordAutoStart.Checked = nudScreenRecorderStartDelay.Enabled = TaskSettings.CaptureSettings.ScreenRecordAutoStart;
            nudScreenRecorderStartDelay.Value = (decimal)TaskSettings.CaptureSettings.ScreenRecordStartDelay;
            cbScreenRecorderShowCursor.Checked = TaskSettings.CaptureSettings.ScreenRecordShowCursor;
            chkRunScreencastCLI.Checked = cboEncoder.Enabled = btnEncoderConfig.Enabled = TaskSettings.CaptureSettings.RunScreencastCLI;
            UpdateVideoEncoders();

            // Actions
            TaskHelpers.AddDefaultExternalPrograms(TaskSettings);
            TaskSettings.ExternalPrograms.ForEach(AddFileAction);

            // Watch folders
            cbWatchFolderEnabled.Checked = TaskSettings.WatchFolderEnabled;

            if (TaskSettings.WatchFolderList == null)
            {
                TaskSettings.WatchFolderList = new List<WatchFolderSettings>();
            }
            else
            {
                foreach (WatchFolderSettings watchFolder in TaskSettings.WatchFolderList)
                {
                    AddWatchFolder(watchFolder);
                }
            }

            // Upload
            cbNameFormatCustomTimeZone.Checked = cbNameFormatTimeZone.Enabled = TaskSettings.UploadSettings.UseCustomTimeZone;
            cbNameFormatTimeZone.Items.AddRange(TimeZoneInfo.GetSystemTimeZones().ToArray());
            for (int i = 0; i < cbNameFormatTimeZone.Items.Count; i++)
            {
                if (cbNameFormatTimeZone.Items[i].Equals(TaskSettings.UploadSettings.CustomTimeZone))
                {
                    cbNameFormatTimeZone.SelectedIndex = i;
                    break;
                }
            }
            txtNameFormatPattern.Text = TaskSettings.UploadSettings.NameFormatPattern;
            txtNameFormatPatternActiveWindow.Text = TaskSettings.UploadSettings.NameFormatPatternActiveWindow;
            CodeMenu.Create<ReplCodeMenuEntry>(txtNameFormatPattern, ReplCodeMenuEntry.n, ReplCodeMenuEntry.t, ReplCodeMenuEntry.pn);
            CodeMenu.Create<ReplCodeMenuEntry>(txtNameFormatPatternActiveWindow, ReplCodeMenuEntry.n);
            cbFileUploadUseNamePattern.Checked = TaskSettings.UploadSettings.FileUploadUseNamePattern;

            // Upload / Clipboard upload
            chkClipboardUploadURLContents.Checked = TaskSettings.UploadSettings.ClipboardUploadURLContents;
            cbClipboardUploadShortenURL.Checked = TaskSettings.UploadSettings.ClipboardUploadShortenURL;
            cbClipboardUploadShareURL.Checked = TaskSettings.UploadSettings.ClipboardUploadShareURL;
            cbClipboardUploadAutoIndexFolder.Checked = TaskSettings.UploadSettings.ClipboardUploadAutoIndexFolder;

            // Indexer
            pgIndexerConfig.SelectedObject = TaskSettings.IndexerSettings;

            // Advanced
            pgTaskSettings.SelectedObject = TaskSettings.AdvancedSettings;

            tttvMain.MainTabControl = tcTaskSettings;

            UpdateDefaultSettingVisibility();

            loaded = true;
        }

        private void UpdateWindowTitle()
        {
            if (IsDefault)
            {
                Text = "ShareX - " + Resources.TaskSettingsForm_UpdateWindowTitle_Task_settings;
            }
            else
            {
                Text = "ShareX - " + string.Format(Resources.TaskSettingsForm_UpdateWindowTitle_Task_settings_for__0_, TaskSettings);
            }
        }

        private void UpdateVideoEncoders()
        {
            cboEncoder.Items.Clear();

            if (Program.Settings.VideoEncoders.Count > 0)
            {
                Program.Settings.VideoEncoders.ForEach(x => cboEncoder.Items.Add(x));
                cboEncoder.SelectedIndex = TaskSettings.CaptureSettings.VideoEncoderSelected.BetweenOrDefault(0, Program.Settings.VideoEncoders.Count - 1);
            }
            else if (!cboEncoder.Items.Contains(ConfigureEncoder))
            {
                cboEncoder.Items.Add(ConfigureEncoder);
                cboEncoder.SelectedIndex = 0;
            }
        }

        private void UpdateDefaultSettingVisibility()
        {
            if (!IsDefault)
            {
                panelGeneral.Enabled = !TaskSettings.UseDefaultGeneralSettings;
                pImage.Enabled = ((Control)tpEffects).Enabled = ((Control)tpThumbnail).Enabled = !TaskSettings.UseDefaultImageSettings;
                pCapture.Enabled = ((Control)tpRegionCapture).Enabled = ((Control)tpScreenRecorder).Enabled = ((Control)tpRectangleAnnotate).Enabled = !TaskSettings.UseDefaultCaptureSettings;
                pActions.Enabled = !TaskSettings.UseDefaultActions;
                pUpload.Enabled = ((Control)tpUploadClipboard).Enabled = !TaskSettings.UseDefaultUploadSettings;
                pgIndexerConfig.Enabled = !TaskSettings.UseDefaultIndexerSettings;
                pgTaskSettings.Enabled = !TaskSettings.UseDefaultAdvancedSettings;
            }
        }

        private void UpdateNameFormatPreviews()
        {
            NameParser nameParser = new NameParser(NameParserType.FileName)
            {
                AutoIncrementNumber = Program.Settings.NameParserAutoIncrementNumber,
                WindowText = Text,
                ProcessName = "ShareX",
                MaxNameLength = TaskSettings.AdvancedSettings.NamePatternMaxLength,
                MaxTitleLength = TaskSettings.AdvancedSettings.NamePatternMaxTitleLength,
                CustomTimeZone = TaskSettings.UploadSettings.UseCustomTimeZone ? TaskSettings.UploadSettings.CustomTimeZone : null
            };

            lblNameFormatPatternPreview.Text = Resources.TaskSettingsForm_txtNameFormatPatternActiveWindow_TextChanged_Preview_ + " " +
                nameParser.Parse(TaskSettings.UploadSettings.NameFormatPattern);

            lblNameFormatPatternPreviewActiveWindow.Text = Resources.TaskSettingsForm_txtNameFormatPatternActiveWindow_TextChanged_Preview_ + " " +
                nameParser.Parse(TaskSettings.UploadSettings.NameFormatPatternActiveWindow);
        }

        private void TaskSettingsForm_Resize(object sender, EventArgs e)
        {
            Refresh();
        }

        #region Task

        private void UpdateDestinationStates()
        {
            if (Program.UploadersConfig != null)
            {
                EnableDisableToolStripMenuItems<ImageDestination>(tsmiImageUploaders);
                EnableDisableToolStripMenuItems<FileDestination>(tsmiImageFileUploaders);
                EnableDisableToolStripMenuItems<TextDestination>(tsmiTextUploaders);
                EnableDisableToolStripMenuItems<FileDestination>(tsmiTextFileUploaders);
                EnableDisableToolStripMenuItems<FileDestination>(tsmiFileUploaders);
                EnableDisableToolStripMenuItems<UrlShortenerType>(tsmiURLShorteners);
                EnableDisableToolStripMenuItems<URLSharingServices>(tsmiURLSharingServices);
                chkOverrideFTP.Enabled = cboFTPaccounts.Enabled = Program.UploadersConfig.FTPAccountList.Count > 1;
                chkOverrideCustomUploader.Enabled = cbOverrideCustomUploader.Enabled = Program.UploadersConfig.CustomUploadersList.Count > 1;
            }
        }

        private void AddEnumItemsContextMenu<T>(Action<T> selectedEnum, params ToolStripDropDown[] parents)
        {
            EnumInfo[] enums = Helpers.GetEnums<T>().OfType<Enum>().Select(x => new EnumInfo(x)).ToArray();

            foreach (ToolStripDropDown parent in parents)
            {
                foreach (EnumInfo enumInfo in enums)
                {
                    ToolStripMenuItem tsmi = new ToolStripMenuItem(enumInfo.Description.Replace("&", "&&"));
                    tsmi.Tag = enumInfo;

                    tsmi.Click += (sender, e) =>
                    {
                        SetEnumCheckedContextMenu(enumInfo, parents);

                        selectedEnum((T)Enum.ToObject(typeof(T), enumInfo.Value));

                        UpdateUploaderMenuNames();
                    };

                    if (!string.IsNullOrEmpty(enumInfo.Category))
                    {
                        ToolStripMenuItem tsmiParent = parent.Items.OfType<ToolStripMenuItem>().FirstOrDefault(x => x.Text == enumInfo.Category);

                        if (tsmiParent == null)
                        {
                            tsmiParent = new ToolStripMenuItem(enumInfo.Category);
                            parent.Items.Add(tsmiParent);
                        }

                        tsmiParent.DropDownItems.Add(tsmi);
                    }
                    else
                    {
                        parent.Items.Add(tsmi);
                    }
                }
            }
        }

        private void SetEnumCheckedContextMenu(Enum value, params ToolStripDropDown[] parents)
        {
            SetEnumCheckedContextMenu(new EnumInfo(value), parents);
        }

        private void SetEnumCheckedContextMenu(EnumInfo enumInfo, params ToolStripDropDown[] parents)
        {
            foreach (ToolStripDropDown parent in parents)
            {
                foreach (ToolStripMenuItem tsmiParent in parent.Items)
                {
                    EnumInfo currentEnumInfo;

                    if (tsmiParent.DropDownItems.Count > 0)
                    {
                        foreach (ToolStripMenuItem tsmiCategoryParent in tsmiParent.DropDownItems)
                        {
                            currentEnumInfo = (EnumInfo)tsmiCategoryParent.Tag;
                            tsmiCategoryParent.Checked = currentEnumInfo.Value.Equals(enumInfo.Value);
                        }
                    }
                    else
                    {
                        currentEnumInfo = (EnumInfo)tsmiParent.Tag;
                        tsmiParent.Checked = currentEnumInfo.Value.Equals(enumInfo.Value);
                    }
                }
            }
        }

        private void AddMultiEnumItemsContextMenu<T>(Action<T> selectedEnum, params ToolStripDropDown[] parents)
        {
            string[] enums = Helpers.GetLocalizedEnumDescriptions<T>().Skip(1).Select(x => x.Replace("&", "&&")).ToArray();

            foreach (ToolStripDropDown parent in parents)
            {
                for (int i = 0; i < enums.Length; i++)
                {
                    ToolStripMenuItem tsmi = new ToolStripMenuItem(enums[i]);

                    int index = i;

                    tsmi.Click += (sender, e) =>
                    {
                        foreach (ToolStripDropDown parent2 in parents)
                        {
                            ToolStripMenuItem tsmi2 = (ToolStripMenuItem)parent2.Items[index];
                            tsmi2.Checked = !tsmi2.Checked;
                        }

                        selectedEnum((T)Enum.ToObject(typeof(T), 1 << index));

                        UpdateUploaderMenuNames();
                    };

                    parent.Items.Add(tsmi);
                }
            }
        }

        private void SetMultiEnumCheckedContextMenu(Enum value, params ToolStripDropDown[] parents)
        {
            for (int i = 0; i < parents[0].Items.Count; i++)
            {
                foreach (ToolStripDropDown parent in parents)
                {
                    ToolStripMenuItem tsmi = (ToolStripMenuItem)parent.Items[i];
                    tsmi.Checked = value.HasFlag(1 << i);
                }
            }
        }

        private void AddEnumItems<T>(Action<T> selectedEnum, params ToolStripDropDownItem[] parents)
        {
            string[] enums = Helpers.GetLocalizedEnumDescriptions<T>();

            foreach (ToolStripDropDownItem parent in parents)
            {
                for (int i = 0; i < enums.Length; i++)
                {
                    ToolStripMenuItem tsmi = new ToolStripMenuItem(enums[i]);

                    int index = i;

                    tsmi.Click += (sender, e) =>
                    {
                        foreach (ToolStripDropDownItem parent2 in parents)
                        {
                            for (int i2 = 0; i2 < enums.Length; i2++)
                            {
                                ToolStripMenuItem tsmi2 = (ToolStripMenuItem)parent2.DropDownItems[i2];
                                tsmi2.Checked = index == i2;
                            }
                        }

                        selectedEnum((T)Enum.ToObject(typeof(T), index));

                        UpdateUploaderMenuNames();
                    };

                    parent.DropDownItems.Add(tsmi);
                }
            }
        }

        private void SetEnumChecked(Enum value, params ToolStripDropDownItem[] parents)
        {
            int index = value.GetIndex();

            foreach (ToolStripDropDownItem parent in parents)
            {
                ((ToolStripMenuItem)parent.DropDownItems[index]).Checked = true;
            }
        }

        private void EnableDisableToolStripMenuItems<T>(params ToolStripDropDownItem[] parents)
        {
            foreach (ToolStripDropDownItem parent in parents)
            {
                for (int i = 0; i < parent.DropDownItems.Count; i++)
                {
                    parent.DropDownItems[i].Enabled = Program.UploadersConfig.IsValid<T>(i);
                }
            }
        }

        private void UpdateUploaderMenuNames()
        {
            btnTask.Text = string.Format(Resources.TaskSettingsForm_UpdateUploaderMenuNames_Task___0_, TaskSettings.Job.GetLocalizedDescription());

            btnAfterCapture.Text = string.Format(Resources.TaskSettingsForm_UpdateUploaderMenuNames_After_capture___0_, string.Join(", ", TaskSettings.AfterCaptureJob.GetFlags<AfterCaptureTasks>().
                Select(x => x.GetLocalizedDescription()).ToArray()));

            btnAfterUpload.Text = string.Format(Resources.TaskSettingsForm_UpdateUploaderMenuNames_After_upload___0_, string.Join(", ", TaskSettings.AfterUploadJob.GetFlags<AfterUploadTasks>().
                Select(x => x.GetLocalizedDescription()).ToArray()));

            string imageUploader = TaskSettings.ImageDestination == ImageDestination.FileUploader ?
                TaskSettings.ImageFileDestination.GetLocalizedDescription() : TaskSettings.ImageDestination.GetLocalizedDescription();
            tsmiImageUploaders.Text = string.Format(Resources.TaskSettingsForm_UpdateUploaderMenuNames_Image_uploader___0_, imageUploader);

            string textUploader = TaskSettings.TextDestination == TextDestination.FileUploader ?
                TaskSettings.TextFileDestination.GetLocalizedDescription() : TaskSettings.TextDestination.GetLocalizedDescription();
            tsmiTextUploaders.Text = string.Format(Resources.TaskSettingsForm_UpdateUploaderMenuNames_Text_uploader___0_, textUploader);

            tsmiFileUploaders.Text = string.Format(Resources.TaskSettingsForm_UpdateUploaderMenuNames_File_uploader___0_, TaskSettings.FileDestination.GetLocalizedDescription());

            tsmiURLShorteners.Text = string.Format(Resources.TaskSettingsForm_UpdateUploaderMenuNames_URL_shortener___0_, TaskSettings.URLShortenerDestination.GetLocalizedDescription());

            tsmiURLSharingServices.Text = string.Format(Resources.TaskSettingsForm_UpdateUploaderMenuNames_URL_sharing_service___0_, TaskSettings.URLSharingServiceDestination.GetLocalizedDescription());
        }

        private void tbDescription_TextChanged(object sender, EventArgs e)
        {
            TaskSettings.Description = tbDescription.Text;
            UpdateWindowTitle();
        }

        private void cbUseDefaultAfterCaptureSettings_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UseDefaultAfterCaptureJob = cbUseDefaultAfterCaptureSettings.Checked;
            btnAfterCapture.Enabled = !TaskSettings.UseDefaultAfterCaptureJob;
        }

        private void cbUseDefaultAfterUploadSettings_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UseDefaultAfterUploadJob = cbUseDefaultAfterUploadSettings.Checked;
            btnAfterUpload.Enabled = !TaskSettings.UseDefaultAfterUploadJob;
        }

        private void cbUseDefaultDestinationSettings_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UseDefaultDestinations = cbUseDefaultDestinationSettings.Checked;
            btnDestinations.Enabled = !TaskSettings.UseDefaultDestinations;
        }

        private void chkOverrideFTP_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.OverrideFTP = chkOverrideFTP.Checked;
            cboFTPaccounts.Enabled = TaskSettings.OverrideFTP;
        }

        private void cboFTPaccounts_SelectedIndexChanged(object sender, EventArgs e)
        {
            TaskSettings.FTPIndex = cboFTPaccounts.SelectedIndex;
        }

        private void chkOverrideCustomUploader_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.OverrideCustomUploader = chkOverrideCustomUploader.Checked;
            cbOverrideCustomUploader.Enabled = TaskSettings.OverrideCustomUploader;
        }

        private void cbOverrideCustomUploader_SelectedIndexChanged(object sender, EventArgs e)
        {
            TaskSettings.CustomUploaderIndex = cbOverrideCustomUploader.SelectedIndex;
        }

        #endregion Task

        #region General

        private void chkUseDefaultGeneralSettings_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UseDefaultGeneralSettings = chkUseDefaultGeneralSettings.Checked;
            UpdateDefaultSettingVisibility();
        }

        private void cbPlaySoundAfterCapture_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.GeneralSettings.PlaySoundAfterCapture = cbPlaySoundAfterCapture.Checked;
        }

        private void cbShowAfterCaptureTasksForm_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.GeneralSettings.ShowAfterCaptureTasksForm = cbShowAfterCaptureTasksForm.Checked;
        }

        private void chkShowBeforeUploadForm_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.GeneralSettings.ShowBeforeUploadForm = chkShowBeforeUploadForm.Checked;
        }

        private void cbPlaySoundAfterUpload_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.GeneralSettings.PlaySoundAfterUpload = cbPlaySoundAfterUpload.Checked;
        }

        private void chkShowAfterUploadForm_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.GeneralSettings.ShowAfterUploadForm = chkShowAfterUploadForm.Checked;
        }

        private void cboPopUpNotification_SelectedIndexChanged(object sender, EventArgs e)
        {
            TaskSettings.GeneralSettings.PopUpNotification = (PopUpNotificationType)cboPopUpNotification.SelectedIndex;
        }

        private void cbHistorySave_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.GeneralSettings.SaveHistory = cbHistorySave.Checked;
        }

        #endregion General

        #region Image

        private void chkUseDefaultImageSettings_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UseDefaultImageSettings = chkUseDefaultImageSettings.Checked;
            UpdateDefaultSettingVisibility();
        }

        private void cbImageFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.ImageFormat = (EImageFormat)cbImageFormat.SelectedIndex;
        }

        private void cbImageGIFQuality_SelectedIndexChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.ImageGIFQuality = (GIFQuality)cbImageGIFQuality.SelectedIndex;
        }

        private void cbImageFormat2_SelectedIndexChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.ImageFormat2 = (EImageFormat)cbImageFormat2.SelectedIndex;
        }

        private void nudImageJPEGQuality_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.ImageJPEGQuality = (int)nudImageJPEGQuality.Value;
        }

        private void nudUseImageFormat2After_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.ImageSizeLimit = (int)nudUseImageFormat2After.Value;
        }

        private void cbImageFileExist_SelectedIndexChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.FileExistAction = (FileExistAction)cbImageFileExist.SelectedIndex;
        }

        private void cbImageEffectOnlyRegionCapture_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.ImageEffectOnlyRegionCapture = cbImageEffectOnlyRegionCapture.Checked;
        }

        private void chkShowImageEffectsWindowAfterCapture_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.ShowImageEffectsWindowAfterCapture = chkShowImageEffectsWindowAfterCapture.Checked;
        }

        private void btnImageEffects_Click(object sender, EventArgs e)
        {
            using (ImageEffectsForm imageEffectsForm = new ImageEffectsForm(ShareXResources.LogoBlack, TaskSettings.ImageSettings.ImageEffects))
            {
                if (imageEffectsForm.ShowDialog() == DialogResult.OK)
                {
                    TaskSettings.ImageSettings.ImageEffects = imageEffectsForm.Effects;
                }
            }
        }

        private void nudThumbnailWidth_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.ThumbnailWidth = (int)nudThumbnailWidth.Value;
        }

        private void nudThumbnailHeight_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.ThumbnailHeight = (int)nudThumbnailHeight.Value;
        }

        private void txtThumbnailName_TextChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.ThumbnailName = txtThumbnailName.Text;
            lblThumbnailNamePreview.Text = "ImageName" + TaskSettings.ImageSettings.ThumbnailName + ".jpg";
        }

        private void cbThumbnailIfSmaller_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.ImageSettings.ThumbnailCheckSize = cbThumbnailIfSmaller.Checked;
        }

        #endregion Image

        #region Capture

        private void chkUseDefaultCaptureSettings_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UseDefaultCaptureSettings = chkUseDefaultCaptureSettings.Checked;
            UpdateDefaultSettingVisibility();
        }

        private void cbCaptureAutoHideTaskbar_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.CaptureAutoHideTaskbar = cbCaptureAutoHideTaskbar.Checked;
        }

        private void nudScreenshotDelay_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.DelayScreenshot = nudScreenshotDelay.Value;
        }

        private void cbScreenshotDelay_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.IsDelayScreenshot = cbScreenshotDelay.Checked;
        }

        private void nudCaptureShadowOffset_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.CaptureShadowOffset = (int)nudCaptureShadowOffset.Value;
        }

        private void cbCaptureClientArea_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.CaptureClientArea = cbCaptureClientArea.Checked;
        }

        private void cbCaptureShadow_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.CaptureShadow = cbCaptureShadow.Checked;
        }

        private void cbShowCursor_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.ShowCursor = cbShowCursor.Checked;
        }

        private void cbCaptureTransparent_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.CaptureTransparent = cbCaptureTransparent.Checked;
            cbCaptureShadow.Enabled = TaskSettings.CaptureSettings.CaptureTransparent;
        }

        private void btnTransmitBoundsFromMonitorToCustomBounds_Click(object sender, EventArgs e)
        {
            MonitorRegion monitorRegionSelected = (MonitorRegion)cboCaptureCustomRegionMonitors.SelectedItem;
            nudCaptureCustomRegionX.Value = monitorRegionSelected.Bounds.X;
            nudCaptureCustomRegionY.Value = monitorRegionSelected.Bounds.Y;
            nudCaptureCustomRegionWidth.Value = monitorRegionSelected.Bounds.Width;
            nudCaptureCustomRegionHeight.Value = monitorRegionSelected.Bounds.Height;
        }

        private void nudScreenRegionX_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.CaptureCustomRegion.X = (int)nudCaptureCustomRegionX.Value;
        }

        private void nudScreenRegionY_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.CaptureCustomRegion.Y = (int)nudCaptureCustomRegionY.Value;
        }

        private void nudScreenRegionWidth_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.CaptureCustomRegion.Width = (int)nudCaptureCustomRegionWidth.Value;
        }

        private void nudScreenRegionHeight_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.CaptureCustomRegion.Height = (int)nudCaptureCustomRegionHeight.Value;
        }

        #endregion Capture

        #region Screen recorder

        private void btnScreenRecorderFFmpegOptions_Click(object sender, EventArgs e)
        {
            ScreencastOptions options = new ScreencastOptions
            {
                FFmpeg = TaskSettings.CaptureSettings.FFmpegOptions,
                ScreenRecordFPS = TaskSettings.CaptureSettings.ScreenRecordFPS,
                GIFFPS = TaskSettings.CaptureSettings.GIFFPS,
                Duration = TaskSettings.CaptureSettings.ScreenRecordFixedDuration ? TaskSettings.CaptureSettings.ScreenRecordDuration : 0,
                OutputPath = "output.mp4",
                CaptureArea = Screen.PrimaryScreen.Bounds,
                DrawCursor = TaskSettings.CaptureSettings.ScreenRecordShowCursor
            };

            using (FFmpegOptionsForm form = new FFmpegOptionsForm(options))
            {
                form.DefaultToolsPath = Path.Combine(Program.ToolsFolder, "ffmpeg.exe");
                form.ShowDialog();
            }
        }

        private void nudScreenRecordFPS_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.ScreenRecordFPS = (int)nudScreenRecordFPS.Value;

            if (TaskSettings.CaptureSettings.ScreenRecordFPS > 30)
            {
                nudScreenRecordFPS.ForeColor = Color.Red;
            }
            else
            {
                nudScreenRecordFPS.ForeColor = SystemColors.WindowText;
            }
        }

        private void nudGIFFPS_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.GIFFPS = (int)nudGIFFPS.Value;

            if (TaskSettings.CaptureSettings.GIFFPS > 15)
            {
                nudGIFFPS.ForeColor = Color.Red;
            }
            else
            {
                nudGIFFPS.ForeColor = SystemColors.WindowText;
            }
        }

        private void cbGIFEncoding_SelectedIndexChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.GIFEncoding = (ScreenRecordGIFEncoding)cbGIFEncoding.SelectedIndex;
        }

        private void cbScreenRecorderFixedDuration_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.ScreenRecordFixedDuration = cbScreenRecorderFixedDuration.Checked;
            nudScreenRecorderDuration.Enabled = TaskSettings.CaptureSettings.ScreenRecordFixedDuration;
        }

        private void nudScreenRecorderDuration_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.ScreenRecordDuration = (float)nudScreenRecorderDuration.Value;
        }

        private void chkScreenRecordAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.ScreenRecordAutoStart = chkScreenRecordAutoStart.Checked;
            nudScreenRecorderStartDelay.Enabled = chkScreenRecordAutoStart.Checked;
        }

        private void nudScreenRecorderStartDelay_ValueChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.ScreenRecordStartDelay = (float)nudScreenRecorderStartDelay.Value;
        }

        private void cbScreenRecorderShowCursor_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.ScreenRecordShowCursor = cbScreenRecorderShowCursor.Checked;
        }

        private void chkRunScreencastCLI_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.RunScreencastCLI = cboEncoder.Enabled = btnEncoderConfig.Enabled = chkRunScreencastCLI.Checked;
        }

        private void cboEncoder_SelectedIndexChanged(object sender, EventArgs e)
        {
            TaskSettings.CaptureSettings.VideoEncoderSelected = cboEncoder.SelectedIndex;
        }

        private void btnEncoderConfig_Click(object sender, EventArgs e)
        {
            using (VideoEncodersForm form = new VideoEncodersForm() { Icon = this.Icon })
            {
                form.ShowDialog();
                UpdateVideoEncoders();
            }
        }

        #endregion Screen recorder

        #region Actions

        private void chkUseDefaultActions_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UseDefaultActions = chkUseDefaultActions.Checked;
            UpdateDefaultSettingVisibility();
        }

        private void btnActionsAdd_Click(object sender, EventArgs e)
        {
            using (ActionsForm form = new ActionsForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    ExternalProgram fileAction = form.FileAction;
                    fileAction.IsActive = true;
                    TaskSettings.ExternalPrograms.Add(fileAction);
                    AddFileAction(fileAction);
                }
            }
        }

        private void AddFileAction(ExternalProgram fileAction)
        {
            ListViewItem lvi = new ListViewItem(fileAction.Name ?? "");
            lvi.Tag = fileAction;
            lvi.Checked = fileAction.IsActive;
            lvi.SubItems.Add(fileAction.Path ?? "");
            lvi.SubItems.Add(fileAction.Args ?? "");
            lvi.SubItems.Add(fileAction.Extensions ?? "");
            lvActions.Items.Add(lvi);
        }

        private void btnActionsEdit_Click(object sender, EventArgs e)
        {
            if (lvActions.SelectedItems.Count > 0)
            {
                ListViewItem lvi = lvActions.SelectedItems[0];
                ExternalProgram fileAction = lvi.Tag as ExternalProgram;

                using (ActionsForm form = new ActionsForm(fileAction))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        lvi.Text = fileAction.Name ?? "";
                        lvi.SubItems[1].Text = fileAction.Path ?? "";
                        lvi.SubItems[2].Text = fileAction.Args ?? "";
                        lvi.SubItems[3].Text = fileAction.Extensions ?? "";
                    }
                }
            }
        }

        private void btnActionsDuplicate_Click(object sender, EventArgs e)
        {
            foreach (ExternalProgram fileAction in lvActions.SelectedItems.Cast<ListViewItem>().Select(x => ((ExternalProgram)x.Tag).Copy()))
            {
                TaskSettings.ExternalPrograms.Add(fileAction);
                AddFileAction(fileAction);
            }
        }

        private void btnActionsRemove_Click(object sender, EventArgs e)
        {
            if (lvActions.SelectedItems.Count > 0)
            {
                ListViewItem lvi = lvActions.SelectedItems[0];
                ExternalProgram fileAction = lvi.Tag as ExternalProgram;

                TaskSettings.ExternalPrograms.Remove(fileAction);
                lvActions.Items.Remove(lvi);
            }
        }

        private void lvActions_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            ExternalProgram fileAction = e.Item.Tag as ExternalProgram;
            fileAction.IsActive = e.Item.Checked;
        }

        private void lvActions_ItemMoved(object sender, int oldIndex, int newIndex)
        {
            TaskSettings.ExternalPrograms.Move(oldIndex, newIndex);
        }

        #endregion Actions

        #region Watch folders

        private void cbWatchFolderEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (loaded)
            {
                TaskSettings.WatchFolderEnabled = cbWatchFolderEnabled.Checked;

                foreach (WatchFolderSettings watchFolderSetting in TaskSettings.WatchFolderList)
                {
                    Program.WatchFolderManager.UpdateWatchFolderState(watchFolderSetting);
                }
            }
        }

        private void btnWatchFolderAdd_Click(object sender, EventArgs e)
        {
            using (WatchFolderForm form = new WatchFolderForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    AddWatchFolder(form.WatchFolder);
                }
            }
        }

        private void lvWatchFolderList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (lvWatchFolderList.SelectedItems.Count > 0)
            {
                ListViewItem lvi = lvWatchFolderList.SelectedItems[0];
                WatchFolderSettings watchFolder = lvi.Tag as WatchFolderSettings;

                using (WatchFolderForm form = new WatchFolderForm(watchFolder))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        lvi.Text = watchFolder.FolderPath ?? "";
                        lvi.SubItems[1].Text = watchFolder.Filter ?? "";
                        lvi.SubItems[2].Text = watchFolder.IncludeSubdirectories.ToString();
                    }
                }
            }
        }

        private void AddWatchFolder(WatchFolderSettings watchFolderSetting)
        {
            if (watchFolderSetting != null)
            {
                Program.WatchFolderManager.AddWatchFolder(watchFolderSetting, TaskSettings);

                ListViewItem lvi = new ListViewItem(watchFolderSetting.FolderPath ?? "");
                lvi.Tag = watchFolderSetting;
                lvi.SubItems.Add(watchFolderSetting.Filter ?? "");
                lvi.SubItems.Add(watchFolderSetting.IncludeSubdirectories.ToString());
                lvWatchFolderList.Items.Add(lvi);
            }
        }

        private void btnWatchFolderRemove_Click(object sender, EventArgs e)
        {
            if (lvWatchFolderList.SelectedItems.Count > 0)
            {
                ListViewItem lvi = lvWatchFolderList.SelectedItems[0];
                WatchFolderSettings watchFolderSetting = lvi.Tag as WatchFolderSettings;
                Program.WatchFolderManager.RemoveWatchFolder(watchFolderSetting);
                lvWatchFolderList.Items.Remove(lvi);
            }
        }

        #endregion Watch folders

        #region Upload

        private void chkUseDefaultUploadSettings_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UseDefaultUploadSettings = chkUseDefaultUploadSettings.Checked;
            UpdateDefaultSettingVisibility();
        }

        private void cbNameFormatCustomTimeZone_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UploadSettings.UseCustomTimeZone = cbNameFormatCustomTimeZone.Checked;
            cbNameFormatTimeZone.Enabled = TaskSettings.UploadSettings.UseCustomTimeZone;
            UpdateNameFormatPreviews();
        }

        private void cbNameFormatTimeZone_SelectedIndexChanged(object sender, EventArgs e)
        {
            TimeZoneInfo timeZoneInfo = cbNameFormatTimeZone.SelectedItem as TimeZoneInfo;

            if (timeZoneInfo != null)
            {
                TaskSettings.UploadSettings.CustomTimeZone = timeZoneInfo;
            }

            UpdateNameFormatPreviews();
        }

        private void txtNameFormatPattern_TextChanged(object sender, EventArgs e)
        {
            TaskSettings.UploadSettings.NameFormatPattern = txtNameFormatPattern.Text;
            UpdateNameFormatPreviews();
        }

        private void txtNameFormatPatternActiveWindow_TextChanged(object sender, EventArgs e)
        {
            TaskSettings.UploadSettings.NameFormatPatternActiveWindow = txtNameFormatPatternActiveWindow.Text;
            UpdateNameFormatPreviews();
        }

        private void btnResetAutoIncrementNumber_Click(object sender, EventArgs e)
        {
            Program.Settings.NameParserAutoIncrementNumber = 0;
            UpdateNameFormatPreviews();
        }

        private void cbFileUploadUseNamePattern_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UploadSettings.FileUploadUseNamePattern = cbFileUploadUseNamePattern.Checked;
        }

        private void chkClipboardUploadContents_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UploadSettings.ClipboardUploadURLContents = chkClipboardUploadURLContents.Checked;
        }

        private void cbClipboardUploadAutoDetectURL_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UploadSettings.ClipboardUploadShortenURL = cbClipboardUploadShortenURL.Checked;
        }

        private void cbClipboardUploadShareURL_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UploadSettings.ClipboardUploadShareURL = cbClipboardUploadShareURL.Checked;
        }

        private void cbClipboardUploadAutoIndexFolder_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UploadSettings.ClipboardUploadAutoIndexFolder = cbClipboardUploadAutoIndexFolder.Checked;
        }

        #endregion Upload

        #region Indexer

        private void chkUseDefaultIndexerSettings_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UseDefaultIndexerSettings = chkUseDefaultIndexerSettings.Checked;
            UpdateDefaultSettingVisibility();
        }

        #endregion Indexer

        #region Advanced

        private void chkUseDefaultAdvancedSettings_CheckedChanged(object sender, EventArgs e)
        {
            TaskSettings.UseDefaultAdvancedSettings = chkUseDefaultAdvancedSettings.Checked;
            UpdateDefaultSettingVisibility();
        }

        #endregion Advanced
    }
}