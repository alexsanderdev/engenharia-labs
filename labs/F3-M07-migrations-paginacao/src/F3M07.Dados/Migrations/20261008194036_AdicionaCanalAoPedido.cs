using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace F3M07.Dados.Migrations
{
    /// <summary>
    /// Gerada com "dotnet ef migrations add AdicionaCanalAoPedido" e EDITADA à mão.
    /// O gerado era um único AddColumn NOT NULL com defaultValue "Web": correto para o schema,
    /// mas pintaria os pedidos antigos como "Web", o que é mentira — eles vieram de antes do canal existir.
    /// Aqui a coluna nasce em três tempos: expand (nullable) → backfill → NOT NULL + DEFAULT.
    /// </summary>
    public partial class AdicionaCanalAoPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Expand: coluna NULLABLE — só metadado, não reescreve a tabela nem segura lock longo.
            migrationBuilder.AddColumn<string>(
                name: "Canal",
                table: "Pedidos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // 2) Backfill: o que já existia recebe um valor que conta a verdade.
            //    EXEC(...) porque no script idempotente a migration inteira é UM lote (batch): o SQL Server
            //    compila o lote antes de rodar o ALTER TABLE e recusaria "Canal" como coluna inexistente.
            //    Em tabela grande, faça em lotes (UPDATE TOP (5000) ... em loop) para não estourar o log nem bloquear.
            migrationBuilder.Sql("EXEC(N'UPDATE Pedidos SET Canal = N''Legado'' WHERE Canal IS NULL');");

            // 3) Agora sim NOT NULL, com DEFAULT 'Web' para quem inserir sem informar o canal
            //    (inclusive a versão ANTIGA da aplicação, que ainda roda durante o deploy).
            migrationBuilder.AlterColumn<string>(
                name: "Canal",
                table: "Pedidos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Web",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down perde a informação de canal: em produção, prefira "rollforward" (nova migration) a rodar Down.
            migrationBuilder.DropColumn(
                name: "Canal",
                table: "Pedidos");
        }
    }
}
