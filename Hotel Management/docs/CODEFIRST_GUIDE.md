# Huong Dan Code First (EF6 + MySQL/TiDB)

Tai lieu nay dung cho project `Hotel Management` (WinForms, .NET Framework 4.8, Entity Framework 6).

## 1. Luong Code First trong project

- `Data/HotelDbContext.cs`: khai bao `DbSet` va map ten bang (`PHONG`, `DATPHONG`, `HOADON`, ...)
- `Migrations/202602141602248_init.cs`: migration tao schema ban dau
- `Migrations/Configuration.cs`: seed du lieu mac dinh + du lieu mau test
- `Program.cs`: bat initializer
  - `Database.SetInitializer(new MigrateDatabaseToLatestVersion<HotelDbContext, Configuration>())`
  - `db.Database.Initialize(false)` de tu dong chay migrate + seed khi mo app

## 2. Chay database lan dau

1. Mo project bang Visual Studio (khuyen nghi VS 2019/2022).
2. Kiem tra `App.config`:
   - connection string `HotelDb`
   - `providerName="MySql.Data.MySqlClient"`
3. Build project 1 lan.
4. Chay app:
   - app se tu dong apply migration hien co
   - app se tu dong seed user, room, customer mau, booking mau, invoice mau

## 3. Tao migration moi khi doi model

Neu ban sua model (vd them cot moi), lam theo thu tu sau:

1. Mo `Tools -> NuGet Package Manager -> Package Manager Console`
2. Chon `Default project`: `Hotel Management`
3. Tao migration:

```powershell
Add-Migration TenMigrationMoi
```

4. Apply migration:

```powershell
Update-Database
```

5. Chay app de verify.

## 4. Seed du lieu mau de test thong ke/bao cao

Ban co 2 cach:

1. Tu dong qua Code First seed:
   - da nam trong `Migrations/Configuration.cs`
   - chay khi migrate hoac khi app startup can cap nhat DB
2. Truc tiep trong giao dien:
   - vao menu `Bao cao`
   - bam nut `Tao du lieu mau`
   - du lieu sinh them theo logic idempotent (neu da co mau thi khong nhan ban)

Du lieu mau du de test:
- `Thong ke dat phong` (DATPHONG)
- `Bao cao doanh thu` (HOADON)
- `Xuat CSV`

## 5. Quy tac status du lieu mau

- `DATPHONG.TrangThai`:
  - `0`: dat truoc
  - `1`: dang o
  - `2`: da tra
- `HOADON.DaThanhToan`:
  - `1`: da thanh toan
  - `0`: chua thanh toan

## 6. Kiem tra nhanh sau khi chay

1. Dang nhap admin de mo menu `Bao cao`.
2. Vao `Thong ke`:
   - co so lieu theo ngay/phong
3. Vao `Bao cao`:
   - co tong hoa don, da thu/chua thu
   - co bang doanh thu theo ngay, theo phong, chi tiet hoa don
4. Bam `Xuat CSV`:
   - file csv duoc tao thanh cong

## 7. Loi thuong gap

- Loi ket noi DB: kiem tra `Server/Port/Uid/Pwd/SslMode` trong `App.config`
- `Add-Migration` khong nhan context:
  - dam bao project startup la `Hotel Management`
  - dam bao build thanh cong truoc khi tao migration
- Seed chay nhieu lan:
  - logic hien tai co check ton tai theo CCCD prefix mau (`9900000000xx`) de tranh duplicate
