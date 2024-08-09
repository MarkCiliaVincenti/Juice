using System;
using Juice.EF;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Juice.MediatR.RequestManager.EF.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class CombinedKey : Migration
    {
        private readonly ISchemaDbContext _schema;
        public CombinedKey() { }

        public CombinedKey(ISchemaDbContext schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ClientRequest",
                schema: _schema.Schema,
                table: "ClientRequest");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "ClientRequest",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClientRequest",
                schema: _schema.Schema,
                table: "ClientRequest",
                columns: new[] { "Id", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ClientRequest",
                schema: _schema.Schema,
                table: "ClientRequest");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "ClientRequest",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClientRequest",
                schema: _schema.Schema,
                table: "ClientRequest",
                column: "Id");
        }
    }
}
