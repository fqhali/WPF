using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace StudentRegistrationApp
{
    public class OrderItem
    {
        public int Id { get; set; }
        public string NamaPelanggan { get; set; } = string.Empty;
        public string Menu { get; set; } = string.Empty;
        public int Jumlah { get; set; }
        public string TipeLayanan { get; set; } = string.Empty;
        public string Tanggal { get; set; } = string.Empty;
        public string OpsiTambahan { get; set; } = string.Empty;
        public string Catatan { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"[#{Id}] {NamaPelanggan} - {Menu} ({Jumlah} porsi) | {TipeLayanan} | {Tanggal} | Tambahan: {OpsiTambahan}";
        }
    }

    public partial class MainWindow : Window
    {
        private readonly string dbPath = "food_orders.db";
        private List<OrderItem> daftarPesanan = new List<OrderItem>();

        public MainWindow()
        {
            InitializeComponent();
            InisialisasiDatabase();
            dpTanggal.SelectedDate = DateTime.Now;
            MuatDataPesanan();
        }

        // ================= DATABASE SETUP =================
        private void InisialisasiDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS Orders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    NamaPelanggan TEXT NOT NULL,
                    Menu TEXT NOT NULL,
                    Jumlah INTEGER NOT NULL,
                    TipeLayanan TEXT NOT NULL,
                    Tanggal TEXT NOT NULL,
                    OpsiTambahan TEXT,
                    Catatan TEXT
                );";

            using var command = new SqliteCommand(createTableSql, connection);
            command.ExecuteNonQuery();
        }

        // ================= EVENT 1: SIMPAN PESANAN =================
        private void BtnSimpan_Click(object sender, RoutedEventArgs e)
        {
            // Validasi Input
            if (string.IsNullOrWhiteSpace(txtNamaPelanggan.Text))
            {
                MessageBox.Show("Nama pelanggan wajib diisi!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNamaPelanggan.Focus();
                return;
            }

            if (cmbMenu.SelectedItem == null)
            {
                MessageBox.Show("Pilih menu makanan/minuman!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtJumlah.Text.Trim(), out int jumlah) || jumlah <= 0)
            {
                MessageBox.Show("Jumlah porsi harus berupa angka dan minimal 1!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtJumlah.Focus();
                return;
            }

            if (!dpTanggal.SelectedDate.HasValue)
            {
                MessageBox.Show("Pilih tanggal pesanan!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nama = txtNamaPelanggan.Text.Trim();
            string menu = (cmbMenu.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
            string tipe = rbDineIn.IsChecked == true ? "Dine In" : "Take Away";
            string tgl = dpTanggal.SelectedDate.Value.ToString("yyyy-MM-dd");

            List<string> opsiList = new List<string>();
            if (chkPedas.IsChecked == true) opsiList.Add("Extra Pedas");
            if (chkTakeAwayPackaging.IsChecked == true) opsiList.Add("Eco Packaging");
            string opsiTambahan = opsiList.Count > 0 ? string.Join(", ", opsiList) : "Standard";

            string catatan = txtCatatan.Text.Trim();

            // Simpan ke SQLite
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                string insertSql = @"
                    INSERT INTO Orders (NamaPelanggan, Menu, Jumlah, TipeLayanan, Tanggal, OpsiTambahan, Catatan)
                    VALUES (@nama, @menu, @jumlah, @tipe, @tgl, @opsi, @catatan);";

                using var command = new SqliteCommand(insertSql, connection);
                command.Parameters.AddWithValue("@nama", nama);
                command.Parameters.AddWithValue("@menu", menu);
                command.Parameters.AddWithValue("@jumlah", jumlah);
                command.Parameters.AddWithValue("@tipe", tipe);
                command.Parameters.AddWithValue("@tgl", tgl);
                command.Parameters.AddWithValue("@opsi", opsiTambahan);
                command.Parameters.AddWithValue("@catatan", catatan);

                command.ExecuteNonQuery();
            }

            MessageBox.Show("Pesanan berhasil disimpan ke database!", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
            ResetForm();
            MuatDataPesanan();
        }

        // ================= EVENT 2: HAPUS PESANAN =================
        private void BtnHapus_Click(object sender, RoutedEventArgs e)
        {
            if (lstPesanan.SelectedItem is OrderItem item)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Apakah Anda yakin ingin menghapus pesanan #{item.Id} atas nama {item.NamaPelanggan}?",
                    "Konfirmasi Hapus",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = new SqliteConnection($"Data Source={dbPath}"))
                    {
                        connection.Open();
                        string deleteSql = "DELETE FROM Orders WHERE Id = @id;";
                        using var command = new SqliteCommand(deleteSql, connection);
                        command.Parameters.AddWithValue("@id", item.Id);
                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("Pesanan berhasil dihapus!", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
                    MuatDataPesanan();
                }
            }
            else
            {
                MessageBox.Show("Pilih pesanan dari daftar terlebih dahulu!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ================= EVENT 3: SEARCH PESANAN =================
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            TampilkanDataList();
        }

        // ================= EVENT 4: SELECTION CHANGED =================
        private void LstPesanan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstPesanan.SelectedItem is OrderItem item)
            {
                // Menampilkan preview catatan di status bar
                txtStatus.Text = $"Dipilih: #{item.Id} - {item.NamaPelanggan} | Catatan: {(string.IsNullOrEmpty(item.Catatan) ? "Tidak ada" : item.Catatan)}";
            }
            else
            {
                TampilkanDataList();
            }
        }

        // ================= EVENT: RESET =================
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        // ================= QUERY & HELPER METHODS =================
        private void MuatDataPesanan()
        {
            daftarPesanan.Clear();

            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                string selectSql = "SELECT Id, NamaPelanggan, Menu, Jumlah, TipeLayanan, Tanggal, OpsiTambahan, Catatan FROM Orders ORDER BY Id DESC;";
                using var command = new SqliteCommand(selectSql, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    daftarPesanan.Add(new OrderItem
                    {
                        Id = reader.GetInt32(0),
                        NamaPelanggan = reader.GetString(1),
                        Menu = reader.GetString(2),
                        Jumlah = reader.GetInt32(3),
                        TipeLayanan = reader.GetString(4),
                        Tanggal = reader.GetString(5),
                        OpsiTambahan = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        Catatan = reader.IsDBNull(7) ? "" : reader.GetString(7)
                    });
                }
            }

            TampilkanDataList();
        }

        private void TampilkanDataList()
        {
            string keyword = txtSearch != null ? txtSearch.Text.Trim().ToLower() : string.Empty;

            var hasil = daftarPesanan.Where(o =>
                string.IsNullOrEmpty(keyword) ||
                o.NamaPelanggan.ToLower().Contains(keyword) ||
                o.Menu.ToLower().Contains(keyword) ||
                o.TipeLayanan.ToLower().Contains(keyword)
            ).ToList();

            lstPesanan.ItemsSource = null;
            lstPesanan.ItemsSource = hasil;

            txtStatus.Text = $"Total Pesanan: {daftarPesanan.Count} (Ditemukan: {hasil.Count})";
        }

        private void ResetForm()
        {
            txtNamaPelanggan.Clear();
            cmbMenu.SelectedIndex = -1;
            txtJumlah.Clear();
            rbDineIn.IsChecked = true;
            dpTanggal.SelectedDate = DateTime.Now;
            chkPedas.IsChecked = false;
            chkTakeAwayPackaging.IsChecked = false;
            txtCatatan.Clear();
            txtNamaPelanggan.Focus();
        }
    }
}