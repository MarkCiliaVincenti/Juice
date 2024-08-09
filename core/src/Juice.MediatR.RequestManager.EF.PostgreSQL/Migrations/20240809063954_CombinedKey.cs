using System;
using Juice.EF;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Juice.MediatR.RequestManager.EF.PostgreSQL.Migrations
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
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

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
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClientRequest",
                schema: _schema.Schema,
                table: "ClientRequest",
                column: "Id");
        }
    }
}
