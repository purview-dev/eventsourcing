using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Purview.EventSourcing.Postgres.Snapshots.EntityFramework.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.EnsureSchema(name: "public");

		migrationBuilder.CreateTable(
			name: "EventStoreSnapshots",
			schema: "public",
			columns: table => new
			{
				Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
				AggregateType = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
				Payload = table.Column<string>(type: "jsonb", nullable: false),
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_EventStoreSnapshots", x => x.Id);
			}
		);

		migrationBuilder
			.CreateIndex(
				name: "IX_EventStoreSnapshots_AggregateType",
				schema: "public",
				table: "EventStoreSnapshots",
				column: "AggregateType"
			)
			.Annotation("Npgsql:IndexInclude", new[] { "Payload" });

		migrationBuilder.Sql(
			"""
			CREATE INDEX "IX_EventStoreSnapshots_Payload_Gin"
			ON "public"."EventStoreSnapshots"
			USING gin ("Payload");
			"""
		);
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_EventStoreSnapshots_Payload_Gin";""");
		migrationBuilder.DropTable(name: "EventStoreSnapshots", schema: "public");
	}
}
