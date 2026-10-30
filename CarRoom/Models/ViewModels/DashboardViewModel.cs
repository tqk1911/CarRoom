
namespace CarRoom.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TongSoXe { get; set; }
        public int TongNguoiDung { get; set; }
        public int TongLichHen { get; set; }
        public int LichHenThangNay { get; set; }
        public int LichHenDangCho { get; set; }
        public int LichHenDaXacNhan { get; set; }
        public int LichHenDaHuy { get; set; }

        public List<DashboardAppointmentItem> LichHenGanDay { get; set; } = new();
        public List<DashboardCarStatistic> ThongKeDongXe { get; set; } = new();
        public List<DashboardDailyStatistic> ThongKeTheoNgay { get; set; } = new();
        public List<DashboardTimeStatistic> ThongKe30Ngay { get; set; } = new();
        public List<DashboardTimeStatistic> ThongKe12Thang { get; set; } = new();

        public List<DashboardSalesStatistic> ThongKeDoanhSo { get; set; } = new();
        public List<DashboardTrafficStatistic> ThongKeTruyCap { get; set; } = new();
    }

    public class DashboardAppointmentItem
    {
        public int Id { get; set; }
        public string HoTenKhach { get; set; } = "";
        public string TenXe { get; set; } = "";
        public DateTime NgayHen { get; set; }
        public string TrangThai { get; set; } = "";
    }

    public class DashboardCarStatistic
    {
        public string TenDongXe { get; set; } = "";
        public int SoLichHen { get; set; }
    }

    public class DashboardDailyStatistic
    {
        public int Ngay { get; set; }
        public int SoLichHen { get; set; }
    }

    public class DashboardTimeStatistic
    {
        public string Nhan { get; set; } = "";
        public int SoLichHen { get; set; }
    }

    public class DashboardSalesStatistic
    {
        public string TenDongXe { get; set; } = "";
        public int SoLuongBan { get; set; }
    }

    public class DashboardTrafficStatistic
    {
        public DateTime Ngay { get; set; }
        public int LuotXem { get; set; }
        public int LuotTruyCap { get; set; }
    }
}
