$desktop = [Environment]::GetFolderPath("Desktop")
$sourceFile = "$desktop\ADBilgi_Kaynak.cs"
$outputExe = "$desktop\ADBilgi.exe"

$csharpCode = @'
using System;
using System.Drawing;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

public class ADForm : Form
{
    private TextBox txtUser;
    private ListBox lstSuggestions;
    private System.Windows.Forms.Timer typingTimer;

    private TextBox txtGroupAction;
    private ListBox lstGroupSuggestions;
    private System.Windows.Forms.Timer groupTypingTimer;

    private Button btnSearch;
    private Button btnUnlock;
    private Button btnResetPassword;
    private Button btnToggleStatus;
    private TextBox txtOutput;
    private ListBox lstGroups;
    private Label lblGroups;
    private Button btnAddGroup;
    private Button btnRemoveGroup;

    private string currentAccount = "";
    private bool currentEnabledState = false;
    private bool suppressSuggestion = false;
    private bool suppressGroupSuggestion = false;

    public ADForm()
    {
        this.Text = "Active Directory Destek Konsolu";
        this.Size = new Size(900, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        Label lbl = new Label() { Text = "Kullanıcı Adı:", Location = new Point(20, 20), Size = new Size(80, 23) };
        this.Controls.Add(lbl);

        txtUser = new TextBox() { Location = new Point(100, 18), Size = new Size(160, 23) };
        txtUser.TextChanged += TxtUser_TextChanged;
        txtUser.KeyDown += TxtUser_KeyDown;
        this.Controls.Add(txtUser);

        lstSuggestions = new ListBox()
        {
            Location = new Point(100, 42),
            Size = new Size(245, 120),
            Visible = false,
            Font = new Font("Segoe UI", 8.5f)
        };
        lstSuggestions.Click += LstSuggestions_Click;
        lstSuggestions.KeyDown += LstSuggestions_KeyDown;
        this.Controls.Add(lstSuggestions);
        lstSuggestions.BringToFront();

        typingTimer = new System.Windows.Forms.Timer();
        typingTimer.Interval = 350;
        typingTimer.Tick += TypingTimer_Tick;

        btnSearch = new Button() { Text = "Sorgula", Location = new Point(270, 16), Size = new Size(75, 26) };
        btnSearch.Click += (s, e) => { HideAllSuggestions(); RunSearch(txtUser.Text.Trim()); };
        this.Controls.Add(btnSearch);

        btnUnlock = new Button() { Text = "Kilidi Aç", Location = new Point(355, 16), Size = new Size(80, 26), Enabled = false, BackColor = Color.FromArgb(230, 245, 230) };
        btnUnlock.Click += BtnUnlock_Click;
        this.Controls.Add(btnUnlock);

        btnResetPassword = new Button() { Text = "Şifre Sıfırla", Location = new Point(445, 16), Size = new Size(95, 26), Enabled = false, BackColor = Color.FromArgb(255, 235, 205) };
        btnResetPassword.Click += BtnResetPassword_Click;
        this.Controls.Add(btnResetPassword);

        btnToggleStatus = new Button() { Text = "Hesap Durumu", Location = new Point(550, 16), Size = new Size(140, 26), Enabled = false };
        btnToggleStatus.Click += BtnToggleStatus_Click;
        this.Controls.Add(btnToggleStatus);

        txtOutput = new TextBox()
        {
            Location = new Point(20, 60),
            Size = new Size(470, 580),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 9.0f)
        };
        txtOutput.Click += (s, e) => HideAllSuggestions();
        this.Controls.Add(txtOutput);

        lblGroups = new Label() { Text = "Üye Olduğu Gruplar (0) - [Çift Tık: Üyeleri Gör]:", Location = new Point(510, 60), Size = new Size(350, 20) };
        this.Controls.Add(lblGroups);

        lstGroups = new ListBox()
        {
            Location = new Point(510, 85),
            Size = new Size(350, 460),
            Font = new Font("Segoe UI", 8.5f)
        };
        lstGroups.SelectedIndexChanged += LstGroups_SelectedIndexChanged;
        lstGroups.DoubleClick += LstGroups_DoubleClick;
        lstGroups.Click += (s, e) => HideAllSuggestions();
        this.Controls.Add(lstGroups);

        // Grup İşlem Alanı
        Label lblGrpAction = new Label() { Text = "Grup Adı:", Location = new Point(510, 555), Size = new Size(65, 20) };
        this.Controls.Add(lblGrpAction);

        txtGroupAction = new TextBox() { Location = new Point(575, 552), Size = new Size(285, 23) };
        txtGroupAction.TextChanged += TxtGroupAction_TextChanged;
        txtGroupAction.KeyDown += TxtGroupAction_KeyDown;
        this.Controls.Add(txtGroupAction);

        lstGroupSuggestions = new ListBox()
        {
            Location = new Point(575, 430),
            Size = new Size(285, 120),
            Visible = false,
            Font = new Font("Segoe UI", 8.5f)
        };
        lstGroupSuggestions.Click += LstGroupSuggestions_Click;
        lstGroupSuggestions.KeyDown += LstGroupSuggestions_KeyDown;
        this.Controls.Add(lstGroupSuggestions);
        lstGroupSuggestions.BringToFront();

        groupTypingTimer = new System.Windows.Forms.Timer();
        groupTypingTimer.Interval = 350;
        groupTypingTimer.Tick += GroupTypingTimer_Tick;

        btnAddGroup = new Button() 
        { 
            Text = "Gruba Ekle (+)", 
            Location = new Point(510, 585), 
            Size = new Size(170, 28), 
            Enabled = false, 
            BackColor = Color.FromArgb(225, 245, 225) 
        };
        btnAddGroup.Click += BtnAddGroup_Click;
        this.Controls.Add(btnAddGroup);

        btnRemoveGroup = new Button() 
        { 
            Text = "Gruptan Çıkar (-)", 
            Location = new Point(690, 585), 
            Size = new Size(170, 28), 
            Enabled = false, 
            BackColor = Color.FromArgb(255, 225, 225) 
        };
        btnRemoveGroup.Click += BtnRemoveGroup_Click;
        this.Controls.Add(btnRemoveGroup);

        this.Click += (s, e) => HideAllSuggestions();
    }

    // Şifre Sıfırlama Penceresi (Kolay Okunabilir Parola Seçeneği ile)
    private void BtnResetPassword_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(currentAccount)) return;

        using (Form resetForm = new Form())
        {
            resetForm.Text = "Şifre Sıfırla - " + currentAccount;
            resetForm.Size = new Size(440, 290);
            resetForm.StartPosition = FormStartPosition.CenterParent;
            resetForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            resetForm.MaximizeBox = false;
            resetForm.MinimizeBox = false;

            Label lblNew = new Label() { Text = "Belirlenecek Geçici Şifre:", Location = new Point(20, 20), Size = new Size(180, 20) };
            resetForm.Controls.Add(lblNew);

            TextBox txtNew = new TextBox() { Location = new Point(20, 45), Size = new Size(260, 23), Font = new Font("Consolas", 11.0f, FontStyle.Bold) };
            resetForm.Controls.Add(txtNew);

            Button btnGenerate = new Button() { Text = "🎲 Yeni Üret", Location = new Point(290, 43), Size = new Size(115, 27) };
            resetForm.Controls.Add(btnGenerate);

            Button btnCopy = new Button() { Text = "📋 Şifreyi Kopyala", Location = new Point(20, 78), Size = new Size(130, 26) };
            resetForm.Controls.Add(btnCopy);

            // Kolay Okunabilir / Karmaşık Şifre Seçimi
            RadioButton rbEasy = new RadioButton() { Text = "Kolay Okunur (Örn: Mavi384!)", Location = new Point(20, 115), Size = new Size(210, 22), Checked = true };
            RadioButton rbComplex = new RadioButton() { Text = "Karmaşık (Örn: kM8!wP3#)", Location = new Point(235, 115), Size = new Size(180, 22) };
            resetForm.Controls.Add(rbEasy);
            resetForm.Controls.Add(rbComplex);

            btnGenerate.Click += (s, ev) =>
            {
                txtNew.Text = rbEasy.Checked ? GenerateEasyReadablePassword() : GenerateComplexPassword(12);
            };

            rbEasy.CheckedChanged += (s, ev) =>
            {
                if (rbEasy.Checked) txtNew.Text = GenerateEasyReadablePassword();
            };

            rbComplex.CheckedChanged += (s, ev) =>
            {
                if (rbComplex.Checked) txtNew.Text = GenerateComplexPassword(12);
            };

            btnCopy.Click += (s, ev) =>
            {
                if (!string.IsNullOrEmpty(txtNew.Text))
                {
                    Clipboard.SetText(txtNew.Text);
                    MessageBox.Show("Şifre panoya kopyalandı:\n\n" + txtNew.Text, "Kopyalandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            // Varsayılan olarak kolay okunur şifre üret
            txtNew.Text = GenerateEasyReadablePassword();

            CheckBox chkForce = new CheckBox() 
            { 
                Text = "İlk oturumda şifre değiştirmeye zorla", 
                Location = new Point(20, 150), 
                Size = new Size(340, 24), 
                Checked = true 
            };
            resetForm.Controls.Add(chkForce);

            Button btnApply = new Button() { Text = "Uygula", Location = new Point(210, 195), Size = new Size(95, 30), DialogResult = DialogResult.OK };
            resetForm.Controls.Add(btnApply);
            resetForm.AcceptButton = btnApply;

            Button btnCancel = new Button() { Text = "İptal", Location = new Point(315, 195), Size = new Size(95, 30), DialogResult = DialogResult.Cancel };
            resetForm.Controls.Add(btnCancel);

            if (resetForm.ShowDialog(this) == DialogResult.OK)
            {
                string newPwd = txtNew.Text.Trim();
                if (string.IsNullOrEmpty(newPwd))
                {
                    MessageBox.Show("Şifre boş bırakılamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
                    {
                        using (UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, currentAccount))
                        {
                            if (user != null)
                            {
                                user.SetPassword(newPwd);
                                if (chkForce.Checked)
                                {
                                    user.ExpirePasswordNow();
                                }
                                user.Save();

                                MessageBox.Show(currentAccount + " kullanıcısının şifresi başarıyla güncellendi!\n\nAtanan Şifre: " + newPwd + 
                                    (chkForce.Checked ? "\n(Kullanıcı ilk oturumunda değiştirmek zorundadır)" : ""), 
                                    "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                
                                RunSearch(currentAccount);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Şifre sıfırlanırken hata oluştu (Domain şifre ilkelerine uymuyor olabilir veya yetkiniz yetersiz):\n\n" + ex.Message, 
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    // Telefonda ve Klavyede En Kolay Aktarılan Parola Modeli: "Kelime + 3 Rakam + Kolay Simge"
    private static string GenerateEasyReadablePassword()
    {
        // Türkçe klavyede rahat basılan, karışmayan temiz kelimeler
        string[] words = new string[] {
            "Mavi", "Yesil", "Beyaz", "Gunes", "Deniz", "Yildiz", 
            "Kartal", "Sahin", "Aslan", "Bulut", "Kuzey", "Guney",
            "Bahar", "Dunya", "Pusula", "Toprak", "Ruzgar", "Zirve"
        };

        // Klavyede yeri çok belirgin olan ve Türkçe karakter içermeyen semboller
        string[] symbols = new string[] { "!", "?", ".", "*", "+" };

        byte[] rnd = new byte[4];
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(rnd);
        }

        string word = words[rnd[0] % words.Length];
        string symbol = symbols[rnd[1] % symbols.Length];
        
        // Karışıklık yaratmasın diye 0 ve 1 kullanılmayan 3 basamaklı sayı (200-999 arası)
        int number = 200 + ((rnd[2] << 8 | rnd[3]) % 799);

        // Örnek: "Mavi482!" veya "Gunes719?"
        return string.Format("{0}{1}{2}", word, number, symbol);
    }

    // Klasik Karmaşık Şifre Üretici
    private static string GenerateComplexPassword(int length)
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ"; 
        const string lower = "abcdefghijkmnpqrstuvwxyz"; 
        const string digits = "23456789";               
        const string specials = "!@#$%*-_=+?";

        char[] chars = new char[length];
        byte[] randomBytes = new byte[length];
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(randomBytes);
        }

        chars[0] = upper[randomBytes[0] % upper.Length];
        chars[1] = lower[randomBytes[1] % lower.Length];
        chars[2] = digits[randomBytes[2] % digits.Length];
        chars[3] = specials[randomBytes[3] % specials.Length];

        string all = upper + lower + digits + specials;
        for (int i = 4; i < length; i++)
        {
            chars[i] = all[randomBytes[i] % all.Length];
        }

        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            byte[] shuffleBytes = new byte[length];
            rng.GetBytes(shuffleBytes);
            for (int i = length - 1; i > 0; i--)
            {
                int j = shuffleBytes[i] % (i + 1);
                char temp = chars[i];
                chars[i] = chars[j];
                chars[j] = temp;
            }
        }

        return new string(chars);
    }

    // Grubun Üyelerini Gösteren Pencere
    private void LstGroups_DoubleClick(object sender, EventArgs e)
    {
        if (lstGroups.SelectedItem == null) return;
        string selectedGroup = lstGroups.SelectedItem.ToString();
        ShowGroupMembersDialog(selectedGroup);
    }

    private void ShowGroupMembersDialog(string groupName)
    {
        using (Form memberForm = new Form())
        {
            memberForm.Text = "Grup Üyeleri - " + groupName;
            memberForm.Size = new Size(480, 530);
            memberForm.StartPosition = FormStartPosition.CenterParent;
            memberForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            memberForm.MaximizeBox = false;
            memberForm.MinimizeBox = false;

            Label lblInfo = new Label() { Text = "Üyeler taranıyor...", Location = new Point(20, 15), Size = new Size(420, 20), Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            memberForm.Controls.Add(lblInfo);

            TextBox txtFilter = new TextBox() { Location = new Point(20, 40), Size = new Size(425, 23) };
            txtFilter.ForeColor = Color.Gray;
            txtFilter.Text = "Üyeler arasında filtrele...";
            memberForm.Controls.Add(txtFilter);

            ListBox lstMembers = new ListBox()
            {
                Location = new Point(20, 70),
                Size = new Size(425, 340),
                Font = new Font("Segoe UI", 9.0f)
            };
            memberForm.Controls.Add(lstMembers);

            Label lblHint = new Label() 
            { 
                Text = "İpucu: Bir [Kullanıcı] satırına çift tıklayarak ana ekranda sorgulayabilirsiniz.", 
                Location = new Point(20, 415), 
                Size = new Size(425, 20), 
                ForeColor = Color.DimGray, 
                Font = new Font("Segoe UI", 8.0f, FontStyle.Italic) 
            };
            memberForm.Controls.Add(lblHint);

            Button btnClose = new Button() { Text = "Kapat", Location = new Point(345, 445), Size = new Size(100, 30), DialogResult = DialogResult.OK };
            memberForm.Controls.Add(btnClose);
            memberForm.AcceptButton = btnClose;

            List<string> allMembers = new List<string>();
            string targetUserToSearch = null;

            lstMembers.DoubleClick += (s, ev) =>
            {
                if (lstMembers.SelectedItem == null) return;
                string selectedItem = lstMembers.SelectedItem.ToString();

                if (selectedItem.StartsWith("[Kullanıcı] "))
                {
                    string withoutPrefix = selectedItem.Substring("[Kullanıcı] ".Length).Trim();
                    string samAccount = withoutPrefix.Split(' ')[0];

                    targetUserToSearch = samAccount;
                    memberForm.DialogResult = DialogResult.OK;
                    memberForm.Close();
                }
                else if (selectedItem.StartsWith("[Grup] "))
                {
                    MessageBox.Show("Seçilen nesne bir gruptur. Yalnızca kullanıcı hesaplarını doğrudan sorgulayabilirsiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            memberForm.Shown += async (s, ev) =>
            {
                allMembers = await Task.Run(() =>
                {
                    List<string> list = new List<string>();
                    try
                    {
                        using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
                        {
                            using (GroupPrincipal group = GroupPrincipal.FindByIdentity(context, groupName))
                            {
                                if (group != null)
                                {
                                    foreach (Principal m in group.GetMembers(false))
                                    {
                                        try
                                        {
                                            string typeBadge = (m is UserPrincipal) ? "[Kullanıcı] " : ((m is GroupPrincipal) ? "[Grup] " : "[Nesne] ");
                                            string display = typeBadge + m.SamAccountName + (!string.IsNullOrEmpty(m.DisplayName) ? " (" + m.DisplayName + ")" : "");
                                            list.Add(display);
                                        }
                                        finally
                                        {
                                            m.Dispose();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        list.Add("HATA: " + ex.Message);
                    }
                    list.Sort();
                    return list;
                });

                lstMembers.Items.Clear();
                foreach (string m in allMembers) lstMembers.Items.Add(m);
                lblInfo.Text = "'" + groupName + "' Grubu (" + allMembers.Count + " Üye):";
            };

            txtFilter.GotFocus += (s, ev) =>
            {
                if (txtFilter.Text == "Üyeler arasında filtrele...")
                {
                    txtFilter.Text = "";
                    txtFilter.ForeColor = Color.Black;
                }
            };

            txtFilter.LostFocus += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtFilter.Text))
                {
                    txtFilter.ForeColor = Color.Gray;
                    txtFilter.Text = "Üyeler arasında filtrele...";
                }
            };

            txtFilter.TextChanged += (s, ev) =>
            {
                if (txtFilter.Text == "Üyeler arasında filtrele...") return;
                string query = txtFilter.Text.Trim().ToLower();

                lstMembers.BeginUpdate();
                lstMembers.Items.Clear();
                foreach (string item in allMembers)
                {
                    if (string.IsNullOrEmpty(query) || item.ToLower().Contains(query))
                    {
                        lstMembers.Items.Add(item);
                    }
                }
                lstMembers.EndUpdate();
            };

            memberForm.ShowDialog(this);

            if (!string.IsNullOrEmpty(targetUserToSearch))
            {
                suppressSuggestion = true;
                txtUser.Text = targetUserToSearch;
                suppressSuggestion = false;
                HideAllSuggestions();
                RunSearch(targetUserToSearch);
            }
        }
    }

    // --- KULLANICI OTOMATİK TAMAMLAMA ---
    private void TxtUser_TextChanged(object sender, EventArgs e)
    {
        if (suppressSuggestion) return;
        typingTimer.Stop();
        if (txtUser.Text.Trim().Length >= 2) typingTimer.Start();
        else HideSuggestions();
    }

    private void TypingTimer_Tick(object sender, EventArgs e)
    {
        typingTimer.Stop();
        string query = txtUser.Text.Trim();
        if (query.Length >= 2) Task.Run(() => FetchUserSuggestions(query));
    }

    private void FetchUserSuggestions(string query)
    {
        List<string> results = new List<string>();
        try
        {
            using (DirectoryEntry root = new DirectoryEntry())
            {
                using (DirectorySearcher searcher = new DirectorySearcher(root))
                {
                    searcher.Filter = string.Format("(&(objectClass=user)(objectCategory=person)(|(sAMAccountName={0}*)(displayName={0}*)))", query);
                    searcher.PropertiesToLoad.Add("sAMAccountName");
                    searcher.PropertiesToLoad.Add("displayName");
                    searcher.SizeLimit = 10;

                    SearchResultCollection matches = searcher.FindAll();
                    foreach (SearchResult entry in matches)
                    {
                        string sam = entry.Properties.Contains("sAMAccountName") ? entry.Properties["sAMAccountName"][0].ToString() : "";
                        string name = entry.Properties.Contains("displayName") ? entry.Properties["displayName"][0].ToString() : "";

                        if (!string.IsNullOrEmpty(sam))
                        {
                            results.Add(string.IsNullOrEmpty(name) ? sam : string.Format("{0} ({1})", sam, name));
                        }
                    }
                }
            }
        }
        catch { }

        this.BeginInvoke(new Action(() =>
        {
            if (txtUser.Text.Trim() == query && results.Count > 0)
            {
                lstSuggestions.Items.Clear();
                foreach (string r in results) lstSuggestions.Items.Add(r);
                lstSuggestions.Visible = true;
                lstSuggestions.BringToFront();
            }
            else HideSuggestions();
        }));
    }

    private void TxtUser_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down && lstSuggestions.Visible && lstSuggestions.Items.Count > 0)
        {
            lstSuggestions.Focus();
            lstSuggestions.SelectedIndex = 0;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Enter)
        {
            HideSuggestions();
            RunSearch(txtUser.Text.Trim());
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            HideSuggestions();
            e.Handled = true;
        }
    }

    private void LstSuggestions_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && lstSuggestions.SelectedItem != null)
        {
            SelectUserSuggestion();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            HideSuggestions();
            txtUser.Focus();
            e.Handled = true;
        }
    }

    private void LstSuggestions_Click(object sender, EventArgs e)
    {
        if (lstSuggestions.SelectedItem != null) SelectUserSuggestion();
    }

    private void SelectUserSuggestion()
    {
        string raw = lstSuggestions.SelectedItem.ToString();
        string sam = raw.Split(' ')[0];
        suppressSuggestion = true;
        txtUser.Text = sam;
        suppressSuggestion = false;
        HideSuggestions();
        txtUser.Focus();
        txtUser.SelectionStart = txtUser.Text.Length;
        RunSearch(sam);
    }

    private void HideSuggestions()
    {
        lstSuggestions.Visible = false;
        lstSuggestions.Items.Clear();
    }

    // --- GRUP OTOMATİK TAMAMLAMA ---
    private void TxtGroupAction_TextChanged(object sender, EventArgs e)
    {
        if (suppressGroupSuggestion) return;
        groupTypingTimer.Stop();
        if (txtGroupAction.Text.Trim().Length >= 2) groupTypingTimer.Start();
        else HideGroupSuggestions();
    }

    private void GroupTypingTimer_Tick(object sender, EventArgs e)
    {
        groupTypingTimer.Stop();
        string query = txtGroupAction.Text.Trim();
        if (query.Length >= 2) Task.Run(() => FetchGroupSuggestions(query));
    }

    private void FetchGroupSuggestions(string query)
    {
        List<string> results = new List<string>();
        try
        {
            using (DirectoryEntry root = new DirectoryEntry())
            {
                using (DirectorySearcher searcher = new DirectorySearcher(root))
                {
                    searcher.Filter = string.Format("(&(objectClass=group)(sAMAccountName={0}*))", query);
                    searcher.PropertiesToLoad.Add("sAMAccountName");
                    searcher.SizeLimit = 12;

                    SearchResultCollection matches = searcher.FindAll();
                    foreach (SearchResult entry in matches)
                    {
                        if (entry.Properties.Contains("sAMAccountName"))
                        {
                            results.Add(entry.Properties["sAMAccountName"][0].ToString());
                        }
                    }
                }
            }
        }
        catch { }

        this.BeginInvoke(new Action(() =>
        {
            if (txtGroupAction.Text.Trim() == query && results.Count > 0)
            {
                lstGroupSuggestions.Items.Clear();
                foreach (string r in results) lstGroupSuggestions.Items.Add(r);
                lstGroupSuggestions.Visible = true;
                lstGroupSuggestions.BringToFront();
            }
            else HideGroupSuggestions();
        }));
    }

    private void TxtGroupAction_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Up && lstGroupSuggestions.Visible && lstGroupSuggestions.Items.Count > 0)
        {
            lstGroupSuggestions.Focus();
            lstGroupSuggestions.SelectedIndex = lstGroupSuggestions.Items.Count - 1;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Down && lstGroupSuggestions.Visible && lstGroupSuggestions.Items.Count > 0)
        {
            lstGroupSuggestions.Focus();
            lstGroupSuggestions.SelectedIndex = 0;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            HideGroupSuggestions();
            e.Handled = true;
        }
    }

    private void LstGroupSuggestions_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && lstGroupSuggestions.SelectedItem != null)
        {
            SelectGroupSuggestion();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            HideGroupSuggestions();
            txtGroupAction.Focus();
            e.Handled = true;
        }
    }

    private void LstGroupSuggestions_Click(object sender, EventArgs e)
    {
        if (lstGroupSuggestions.SelectedItem != null) SelectGroupSuggestion();
    }

    private void SelectGroupSuggestion()
    {
        string selectedGroup = lstGroupSuggestions.SelectedItem.ToString();
        suppressGroupSuggestion = true;
        txtGroupAction.Text = selectedGroup;
        suppressGroupSuggestion = false;
        HideGroupSuggestions();
        txtGroupAction.Focus();
        txtGroupAction.SelectionStart = txtGroupAction.Text.Length;
    }

    private void HideGroupSuggestions()
    {
        lstGroupSuggestions.Visible = false;
        lstGroupSuggestions.Items.Clear();
    }

    private void HideAllSuggestions()
    {
        HideSuggestions();
        HideGroupSuggestions();
    }

    private void LstGroups_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lstGroups.SelectedItem != null)
        {
            suppressGroupSuggestion = true;
            txtGroupAction.Text = lstGroups.SelectedItem.ToString();
            suppressGroupSuggestion = false;
            HideGroupSuggestions();
        }
    }

    // --- KULLANICI DETAY SORGULAMA ---
    private void RunSearch(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            MessageBox.Show("Lütfen bir kullanıcı adı girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        txtOutput.Text = "Sorgulanıyor, lütfen bekleyin...";
        lstGroups.Items.Clear();
        btnUnlock.Enabled = false;
        btnResetPassword.Enabled = false;
        btnToggleStatus.Enabled = false;
        btnAddGroup.Enabled = false;
        btnRemoveGroup.Enabled = false;
        Application.DoEvents();

        try
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
            {
                using (UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username))
                {
                    if (user == null)
                    {
                        txtOutput.Text = "HATA: Kullanıcı bulunamadı.";
                        return;
                    }

                    currentAccount = user.SamAccountName;
                    bool isLocked = user.IsAccountLockedOut();
                    btnUnlock.Enabled = isLocked;
                    btnResetPassword.Enabled = true;
                    btnAddGroup.Enabled = true;
                    btnRemoveGroup.Enabled = true;

                    currentEnabledState = user.Enabled.HasValue && user.Enabled.Value;
                    btnToggleStatus.Enabled = true;
                    if (currentEnabledState)
                    {
                        btnToggleStatus.Text = "Hesabı Kapat (Disable)";
                        btnToggleStatus.BackColor = Color.FromArgb(255, 218, 221);
                    }
                    else
                    {
                        btnToggleStatus.Text = "Hesabı Aç (Enable)";
                        btnToggleStatus.BackColor = Color.FromArgb(215, 245, 215);
                    }

                    string displayName = user.DisplayName ?? "(Tanımlı Değil)";
                    string email = user.EmailAddress ?? "(Yok)";
                    string phone = user.VoiceTelephoneNumber ?? "(Yok)";
                    string status = currentEnabledState ? "Aktif" : "Devre Dışı (Disabled)";
                    string lastPwdSet = user.LastPasswordSet.HasValue ? user.LastPasswordSet.Value.ToString("dd.MM.yyyy HH:mm") : "Hiç değişmedi";
                    string badLogon = user.LastBadPasswordAttempt.HasValue ? user.LastBadPasswordAttempt.Value.ToString("dd.MM.yyyy HH:mm") : "(Yok)";

                    string expiryText = "Belirsiz";
                    if (user.PasswordNeverExpires)
                    {
                        expiryText = "Süresi dolmuyor (Never Expires)";
                    }
                    else
                    {
                        try
                        {
                            DirectoryEntry de = user.GetUnderlyingObject() as DirectoryEntry;
                            if (de != null)
                            {
                                de.RefreshCache(new string[] { "msDS-UserPasswordExpiryTimeComputed" });
                                if (de.Properties.Contains("msDS-UserPasswordExpiryTimeComputed"))
                                {
                                    object val = de.Properties["msDS-UserPasswordExpiryTimeComputed"].Value;
                                    long fileTime = ConvertToLong(val);
                                    if (fileTime > 0 && fileTime != Int64.MaxValue)
                                        expiryText = DateTime.FromFileTime(fileTime).ToString("dd.MM.yyyy HH:mm");
                                    else
                                        expiryText = "Süresi dolmuyor";
                                }
                            }
                        }
                        catch { expiryText = "Hesaplanamadı"; }
                    }

                    string title = "(Yok)";
                    string department = "(Yok)";
                    string manager = "(Yok)";
                    string readableOuPath = "(Bulunamadı)";
                    string distinguishedName = "(Bulunamadı)";

                    try
                    {
                        DirectoryEntry de = user.GetUnderlyingObject() as DirectoryEntry;
                        if (de != null)
                        {
                            if (de.Properties.Contains("distinguishedName"))
                            {
                                distinguishedName = de.Properties["distinguishedName"].Value.ToString();
                                readableOuPath = ParseOuHierarchy(distinguishedName);
                            }

                            if (de.Properties.Contains("title")) title = de.Properties["title"].Value.ToString();
                            if (de.Properties.Contains("department")) department = de.Properties["department"].Value.ToString();
                            if (de.Properties.Contains("manager"))
                            {
                                string mgrDn = de.Properties["manager"].Value.ToString();
                                string[] mgrParts = mgrDn.Split(',');
                                if (mgrParts.Length > 0 && mgrParts[0].StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                                    manager = mgrParts[0].Substring(3);
                            }
                        }
                    }
                    catch { }

                    List<string> groupNames = new List<string>();
                    try
                    {
                        using (var groups = user.GetGroups())
                        {
                            foreach (Principal p in groups)
                            {
                                try { groupNames.Add(p.Name); }
                                finally { p.Dispose(); }
                            }
                        }
                    }
                    catch
                    {
                        DirectoryEntry de = user.GetUnderlyingObject() as DirectoryEntry;
                        if (de != null && de.Properties.Contains("memberOf"))
                        {
                            foreach (object grp in de.Properties["memberOf"])
                            {
                                string grpStr = grp.ToString();
                                string[] parts = grpStr.Split(',');
                                if (parts.Length > 0 && parts[0].StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                                    groupNames.Add(parts[0].Substring(3));
                            }
                        }
                    }

                    groupNames.Sort();
                    foreach (var g in groupNames) lstGroups.Items.Add(g);
                    lblGroups.Text = "Üye Olduğu Gruplar (" + groupNames.Count + ") - [Çift Tık: Üyeleri Gör]:";

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("=== HESAP DURUMU ===");
                    sb.AppendLine("Kullanıcı Adı    : " + currentAccount);
                    sb.AppendLine("Ad Soyad         : " + displayName);
                    sb.AppendLine("Hesap Durumu     : " + status);
                    sb.AppendLine("Kilitli mi?      : " + (isLocked ? "EVET (KİLİTLİ!)" : "Hayır"));
                    sb.AppendLine();
                    sb.AppendLine("=== KONUM / OU BİLGİSİ ===");
                    sb.AppendLine("OU Yolu          : " + readableOuPath);
                    sb.AppendLine("Tam DN           : " + distinguishedName);
                    sb.AppendLine();
                    sb.AppendLine("=== ŞİFRE BİLGİLERİ ===");
                    sb.AppendLine("Son Değişim      : " + lastPwdSet);
                    sb.AppendLine("Şifre Bitiş      : " + expiryText);
                    sb.AppendLine("Hatalı Giriş     : " + badLogon);
                    sb.AppendLine();
                    sb.AppendLine("=== KURUM BİLGİLERİ ===");
                    sb.AppendLine("Unvan            : " + title);
                    sb.AppendLine("Departman        : " + department);
                    sb.AppendLine("Yönetici         : " + manager);
                    sb.AppendLine("E-Posta          : " + email);
                    sb.AppendLine("Telefon          : " + phone);

                    txtOutput.Text = sb.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            txtOutput.Text = "HATA: " + ex.Message;
        }
    }

    private void BtnAddGroup_Click(object sender, EventArgs e)
    {
        string groupName = txtGroupAction.Text.Trim();
        if (string.IsNullOrEmpty(currentAccount)) return;

        if (string.IsNullOrEmpty(groupName))
        {
            MessageBox.Show("Lütfen eklenecek grup adını girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
            {
                using (GroupPrincipal group = GroupPrincipal.FindByIdentity(context, groupName))
                {
                    if (group == null)
                    {
                        MessageBox.Show("'" + groupName + "' adında bir Active Directory grubu bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    using (UserPrincipal user = UserPrincipal.FindByIdentity(context, currentAccount))
                    {
                        if (user != null)
                        {
                            if (user.IsMemberOf(group))
                            {
                                MessageBox.Show(currentAccount + " kullanıcısı zaten '" + groupName + "' grubuna üye.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            group.Members.Add(user);
                            group.Save();

                            MessageBox.Show(currentAccount + " kullanıcısı '" + groupName + "' grubuna başarıyla eklendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtGroupAction.Clear();
                            RunSearch(currentAccount);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Gruba eklenirken hata oluştu:\n\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnRemoveGroup_Click(object sender, EventArgs e)
    {
        string groupName = txtGroupAction.Text.Trim();
        if (string.IsNullOrEmpty(currentAccount)) return;

        if (string.IsNullOrEmpty(groupName))
        {
            MessageBox.Show("Lütfen çıkarılacak grup adını girin veya listeden seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (groupName.Equals("Domain Users", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("'Domain Users' birincil gruptur; arayüzden çıkarılamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult confirm = MessageBox.Show(
            "'" + currentAccount + "' kullanıcısını '" + groupName + "' grubundan çıkarmak istediğinize emin misiniz?",
            "Onay",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (confirm != DialogResult.Yes) return;

        try
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
            {
                using (GroupPrincipal group = GroupPrincipal.FindByIdentity(context, groupName))
                {
                    if (group == null)
                    {
                        MessageBox.Show("'" + groupName + "' adında bir grup bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    using (UserPrincipal user = UserPrincipal.FindByIdentity(context, currentAccount))
                    {
                        if (user != null)
                        {
                            if (!user.IsMemberOf(group))
                            {
                                MessageBox.Show(currentAccount + " kullanıcısı zaten bu grupta yer almıyor.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            group.Members.Remove(user);
                            group.Save();

                            MessageBox.Show(currentAccount + " kullanıcısı '" + groupName + "' grubundan çıkarıldı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtGroupAction.Clear();
                            RunSearch(currentAccount);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Gruptan çıkarılırken hata oluştu:\n\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string ParseOuHierarchy(string dn)
    {
        if (string.IsNullOrEmpty(dn)) return "(Yok)";
        
        string[] parts = dn.Split(',');
        List<string> ouList = new List<string>();

        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (trimmed.StartsWith("OU=", StringComparison.OrdinalIgnoreCase))
            {
                ouList.Add(trimmed.Substring(3));
            }
            else if (trimmed.StartsWith("CN=Users", StringComparison.OrdinalIgnoreCase))
            {
                ouList.Add("Users Container");
            }
        }

        if (ouList.Count == 0) return "Domain Kök Dizini (Root / Builtin)";

        ouList.Reverse();
        return string.Join("  >  ", ouList.ToArray());
    }

    private void BtnToggleStatus_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(currentAccount)) return;

        string actionName = currentEnabledState ? "devre dışı bırakmak" : "aktifleştirmek";
        DialogResult confirm = MessageBox.Show(
            "'" + currentAccount + "' hesabını " + actionName + " istediğinize emin misiniz?",
            "Onay",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (confirm != DialogResult.Yes) return;

        try
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
            {
                using (UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, currentAccount))
                {
                    if (user != null)
                    {
                        user.Enabled = !currentEnabledState;
                        user.Save();

                        string resultMsg = !currentEnabledState 
                            ? "'" + currentAccount + "' hesabı başarıyla AKTİFLEŞTİRİLDİ." 
                            : "'" + currentAccount + "' hesabı başarıyla DEVRE DIŞI BIRAKILDI.";

                        MessageBox.Show(resultMsg, "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RunSearch(currentAccount);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Hesap durumu güncellenirken hata:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnUnlock_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(currentAccount)) return;

        try
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
            {
                using (UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, currentAccount))
                {
                    if (user != null)
                    {
                        user.UnlockAccount();
                        MessageBox.Show(currentAccount + " kullanıcısının hesap kilidi açıldı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RunSearch(currentAccount);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Kilit açılırken hata:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private long ConvertToLong(object val)
    {
        if (val == null) return 0;
        if (val is long) return (long)val;
        try
        {
            long high = Convert.ToInt64(val.GetType().InvokeMember("HighPart", System.Reflection.BindingFlags.GetProperty, null, val, null));
            long low = (uint)Convert.ToInt32(val.GetType().InvokeMember("LowPart", System.Reflection.BindingFlags.GetProperty, null, val, null));
            return (high << 32) + low;
        }
        catch { return 0; }
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new ADForm());
    }
}
'@

[System.IO.File]::WriteAllText($sourceFile, $csharpCode, [System.Text.Encoding]::UTF8)

$cscPath = Join-Path ([System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) "csc.exe"

$compileParams = @(
    "/target:winexe",
    "/out:`"$outputExe`"",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.DirectoryServices.dll",
    "/reference:System.DirectoryServices.AccountManagement.dll",
    "`"$sourceFile`""
)

Start-Process -FilePath $cscPath -ArgumentList $compileParams -NoNewWindow -Wait

if (Test-Path $outputExe) {
    Remove-Item $sourceFile -ErrorAction SilentlyContinue
    Write-Host "`nADBilgi.exe başarıyla oluşturuldu ve masaüstüne kaydedildi!" -ForegroundColor Green
} else {
    Write-Host "`nDerleme hatası oluştu." -ForegroundColor Red
}
