using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace testpayment6._0.Models;

public partial class DbluanvantotnghiepContext : DbContext
{
    public DbluanvantotnghiepContext()
    {
    }

    public DbluanvantotnghiepContext(DbContextOptions<DbluanvantotnghiepContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<ContactForm> ContactForms { get; set; }

    public virtual DbSet<GuestOrderFood> GuestOrderFoods { get; set; }

    public virtual DbSet<GuestOrderFoodDetail> GuestOrderFoodDetails { get; set; }

    public virtual DbSet<GuestUser> GuestUsers { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<OrderFoodDetail> OrderFoodDetails { get; set; }

    public virtual DbSet<OrderTable> OrderTables { get; set; }

    public virtual DbSet<OrderTablesDetail> OrderTablesDetails { get; set; }

    public virtual DbSet<PaymentResult> PaymentResults { get; set; }

    public virtual DbSet<Region> Regions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Table> Tables { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=database-1.cbqiyiaaa98a.ap-southeast-1.rds.amazonaws.com;database=dbluanvantotnghiep;user id=admin;password=tuhoami9998", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.39-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PRIMARY");

            entity.ToTable("category");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(50)
                .HasColumnName("category_name")
                .UseCollation("utf8mb4_0900_ai_ci");
        });

        modelBuilder.Entity<ContactForm>(entity =>
        {
            entity.HasKey(e => e.ContactId).HasName("PRIMARY");

            entity.ToTable("contact_form");

            entity.HasIndex(e => e.UserId, "fk_contact_form_user");

            entity.Property(e => e.ContactId).HasColumnName("contact_id");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("createAt");
            entity.Property(e => e.UserId)
                .HasMaxLength(20)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.ContactForms)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contact_form_user");
        });

        modelBuilder.Entity<GuestOrderFood>(entity =>
        {
            entity.HasKey(e => e.GuestOrderFoodId).HasName("PRIMARY");

            entity.ToTable("GuestOrderFood");

            entity.HasIndex(e => e.GuestId, "fk_GuestOrderFood_guest");

            entity.Property(e => e.GuestOrderFoodId).HasColumnName("guestOrderFood_id");
            entity.Property(e => e.GuestId).HasColumnName("guest_id");
            entity.Property(e => e.OrderTime)
                .HasColumnType("datetime")
                .HasColumnName("order_time");

            entity.HasOne(d => d.Guest).WithMany(p => p.GuestOrderFoods)
                .HasForeignKey(d => d.GuestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_GuestOrderFood_guest");
        });

        modelBuilder.Entity<GuestOrderFoodDetail>(entity =>
        {
            entity.HasKey(e => e.GuestOrderFoodDetailsId).HasName("PRIMARY");

            entity.HasIndex(e => e.DishId, "fk_GuestOrderFoodDetails_menu");

            entity.HasIndex(e => e.GuestOrderFoodId, "fk_GuestOrderFoodDetails_order");

            entity.Property(e => e.GuestOrderFoodDetailsId).HasColumnName("guestOrderFoodDetails_id");
            entity.Property(e => e.DishId)
                .HasMaxLength(5)
                .HasColumnName("dish_id");
            entity.Property(e => e.GuestOrderFoodId).HasColumnName("guestOrderFood_id");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.Quantity)
                .HasDefaultValueSql("'0'")
                .HasColumnName("quantity");

            entity.HasOne(d => d.Dish).WithMany(p => p.GuestOrderFoodDetails)
                .HasForeignKey(d => d.DishId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_GuestOrderFoodDetails_menu");

            entity.HasOne(d => d.GuestOrderFood).WithMany(p => p.GuestOrderFoodDetails)
                .HasForeignKey(d => d.GuestOrderFoodId)
                .HasConstraintName("fk_GuestOrderFoodDetails_order");
        });

        modelBuilder.Entity<GuestUser>(entity =>
        {
            entity.HasKey(e => e.GuestId).HasName("PRIMARY");

            entity.ToTable("guest_user");

            entity.Property(e => e.GuestId).HasColumnName("guest_id");
            entity.Property(e => e.GuestName)
                .HasMaxLength(50)
                .HasColumnName("guest_name")
                .UseCollation("utf8mb4_0900_ai_ci");
            entity.Property(e => e.PhoneNumber).HasMaxLength(15);
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasKey(e => e.DishId).HasName("PRIMARY");

            entity.ToTable("menu");

            entity.HasIndex(e => e.CategoryId, "fk_menu_category");

            entity.HasIndex(e => e.RegionId, "fk_menu_region");

            entity.Property(e => e.DishId)
                .HasMaxLength(5)
                .HasColumnName("dish_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Descriptions)
                .HasColumnType("text")
                .HasColumnName("descriptions");
            entity.Property(e => e.DishName)
                .HasMaxLength(255)
                .HasColumnName("dish_name");
            entity.Property(e => e.Images)
                .HasMaxLength(255)
                .HasColumnName("images");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.RegionId).HasColumnName("region_id");

            entity.HasOne(d => d.Category).WithMany(p => p.Menus)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_menu_category");

            entity.HasOne(d => d.Region).WithMany(p => p.Menus)
                .HasForeignKey(d => d.RegionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_menu_region");
        });

        modelBuilder.Entity<OrderFoodDetail>(entity =>
        {
            entity.HasKey(e => e.OrderFoodDetailsId).HasName("PRIMARY");

            entity.ToTable("orderFoodDetails");

            entity.HasIndex(e => e.DishId, "fk_orderFoodDetails_menu");

            entity.HasIndex(e => e.OrderTableId, "fk_orderFoodDetails_order");

            entity.Property(e => e.OrderFoodDetailsId).HasColumnName("orderFoodDetailsId");
            entity.Property(e => e.DishId)
                .HasMaxLength(5)
                .HasColumnName("dish_id");
            entity.Property(e => e.Note)
                .HasColumnType("text")
                .HasColumnName("note");
            entity.Property(e => e.OrderTableId).HasColumnName("orderTableId");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Dish).WithMany(p => p.OrderFoodDetails)
                .HasForeignKey(d => d.DishId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_orderFoodDetails_menu");

            entity.HasOne(d => d.OrderTable).WithMany(p => p.OrderFoodDetails)
                .HasForeignKey(d => d.OrderTableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_orderFoodDetails_order");
        });

        modelBuilder.Entity<OrderTable>(entity =>
        {
            entity.HasKey(e => e.OrderTableId).HasName("PRIMARY");

            entity.ToTable("orderTables");

            entity.HasIndex(e => e.UserId, "fk_ordertables_users");

            entity.Property(e => e.OrderTableId).HasColumnName("orderTableId");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("orderDate");
            entity.Property(e => e.StartingTime)
                .HasColumnType("datetime")
                .HasColumnName("starting_time");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(10, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.TotalDeposit)
                .HasPrecision(10, 2)
                .HasColumnName("total_deposit");
            entity.Property(e => e.UserId)
                .HasMaxLength(20)
                .HasColumnName("user_id");
            entity.Property(e => e.isCancel)
                .HasDefaultValueSql("'0'")
                .HasColumnName("isCancel");

            entity.HasOne(d => d.User).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_ordertables_users");
        });

        modelBuilder.Entity<OrderTablesDetail>(entity =>
        {
            entity.HasKey(e => e.OrderTablesDetailsId).HasName("PRIMARY");

            entity.ToTable("orderTablesDetails");

            entity.HasIndex(e => e.OrderTableId, "fk_orderTablesDetails_order");

            entity.HasIndex(e => e.TableId, "fk_orderTablesDetails_table");

            entity.Property(e => e.OrderTablesDetailsId).HasColumnName("orderTablesDetailsId");
            entity.Property(e => e.OrderTableId).HasColumnName("orderTableId");
            entity.Property(e => e.TableId).HasColumnName("table_id");

            entity.HasOne(d => d.OrderTable).WithMany(p => p.OrderTablesDetails)
                .HasForeignKey(d => d.OrderTableId)
                .HasConstraintName("fk_orderTablesDetails_order");

            entity.HasOne(d => d.Table).WithMany(p => p.OrderTablesDetails)
                .HasForeignKey(d => d.TableId)
                .HasConstraintName("fk_orderTablesDetails_table");
        });

        modelBuilder.Entity<PaymentResult>(entity =>
        {
            entity.HasKey(e => e.PaymentResultId).HasName("PRIMARY");

            entity.HasIndex(e => e.OrderTableId, "fk_PaymentResults_order");

            entity.Property(e => e.BankCode).HasMaxLength(20);
            entity.Property(e => e.BankTransactionId).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.OrderTableId).HasColumnName("orderTableId");
            entity.Property(e => e.PaymentMethod).HasMaxLength(100);
            entity.Property(e => e.ResponseDescription).HasMaxLength(255);
            entity.Property(e => e.Timestamp).HasColumnType("datetime");
            entity.Property(e => e.TransactionStatusDescription).HasMaxLength(255);

            entity.HasOne(d => d.OrderTable).WithMany(p => p.PaymentResults)
                .HasForeignKey(d => d.OrderTableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_PaymentResults_order");
        });

        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.RegionId).HasName("PRIMARY");

            entity.ToTable("region");

            entity.Property(e => e.RegionId).HasColumnName("region_id");
            entity.Property(e => e.RegionName)
                .HasMaxLength(50)
                .HasColumnName("region_name")
                .UseCollation("utf8mb4_0900_ai_ci");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RolesId).HasName("PRIMARY");

            entity.ToTable("roles");

            entity.Property(e => e.RolesId)
                .ValueGeneratedNever()
                .HasColumnName("roles_id");
            entity.Property(e => e.RolesDescription)
                .HasMaxLength(100)
                .HasColumnName("roles_description");
        });

        modelBuilder.Entity<Table>(entity =>
        {
            entity.HasKey(e => e.TableId).HasName("PRIMARY");

            entity.ToTable("tables");

            entity.HasIndex(e => e.RegionId, "fk_tables_region");

            entity.Property(e => e.TableId).HasColumnName("table_id");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.Deposit)
                .HasPrecision(10, 2)
                .HasColumnName("deposit");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.RegionId).HasColumnName("region_id");

            entity.HasOne(d => d.Region).WithMany(p => p.Tables)
                .HasForeignKey(d => d.RegionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tables_region");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.RolesId, "fk_users_roles");

            entity.Property(e => e.UserId)
                .HasMaxLength(20)
                .HasColumnName("user_id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("createAt");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(10)
                .HasColumnName("customer_name");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .HasColumnName("phone_number");
            entity.Property(e => e.RolesId).HasColumnName("roles_id");
            entity.Property(e => e.UPassword)
                .HasMaxLength(50)
                .HasColumnName("u_password");

            entity.HasOne(d => d.Roles).WithMany(p => p.Users)
                .HasForeignKey(d => d.RolesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_users_roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
