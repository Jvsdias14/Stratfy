using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace STRATFY.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cartao> Cartoes { get; set; }

    public virtual DbSet<Categoria> Categoria { get; set; }

    public virtual DbSet<Dashboard> Dashboards { get; set; }

    public virtual DbSet<Extrato> Extratos { get; set; }

    public virtual DbSet<Grafico> Graficos { get; set; }

    public virtual DbSet<Movimentacao> Movimentacaos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost\\sqlexpress;Database=Stratfy;Integrated Security=true;MultipleActiveResultSets=True;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cartao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cartoes__3213E83F7BE4EBD1");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Campo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Cor)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DashboardId).HasColumnName("Dashboard_id");
            entity.Property(e => e.Nome)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TipoAgregacao)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Dashboard).WithMany(p => p.Cartoes)
                .HasForeignKey(d => d.DashboardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Cartoes__Dashboa__59FA5E80");
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3213E83F944552C4");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Dashboard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Dashboar__3213E83F6FE3581F");

            entity.ToTable("Dashboard");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Descricao)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ExtratoId).HasColumnName("Extrato_id");

            entity.HasOne(d => d.Extrato).WithMany(p => p.Dashboards)
                .HasForeignKey(d => d.ExtratoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Dashboard__Extra__5070F446");
        });

        modelBuilder.Entity<Extrato>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Extrato__3213E83F0A8FF8C6");

            entity.ToTable("Extrato");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DataCriacao)
                .HasColumnType("date");
                //.HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UsuarioId).HasColumnName("Usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Extratos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Extrato__Usuario__4D94879B");
            
        });

        modelBuilder.Entity<Grafico>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Graficos__3213E83F0600F989");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Campo1)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Campo2)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Cor)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DashboardId).HasColumnName("Dashboard_id");
            entity.Property(e => e.Tipo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Titulo)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Dashboard).WithMany(p => p.Graficos)
                .HasForeignKey(d => d.DashboardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Graficos__Dashbo__571DF1D5");
        });

        modelBuilder.Entity<Movimentacao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Moviment__3213E83F13652FBF");

            entity.ToTable("Movimentacao");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoriaId).HasColumnName("Categoria_id");
            entity.Property(e => e.Descricao)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ExtratoId).HasColumnName("Extrato_id");
            entity.Property(e => e.Tipo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Valor).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Movimentacaos)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movimenta__Categ__5441852A");

            entity.HasOne(d => d.Extrato).WithMany(p => p.Movimentacaos)
                .HasForeignKey(d => d.ExtratoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movimenta__Extra__534D60F1");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuario__3213E83FD30C7490");

            entity.ToTable("Usuario");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Nome)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Senha)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
