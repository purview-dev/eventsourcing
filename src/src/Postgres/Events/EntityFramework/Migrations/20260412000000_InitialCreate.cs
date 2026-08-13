using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Purview.EventSourcing.Postgres.Events.EntityFramework.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.EnsureSchema(name: "public");

		migrationBuilder.CreateTable(
			name: "EventStoreEvents",
			schema: "public",
			columns: table => new
			{
				Id = table.Column<string>(
					type: "character varying(450)",
					maxLength: 450,
					nullable: false
				),
				EntityType = table.Column<int>(type: "integer", nullable: false),
				AggregateId = table.Column<string>(
					type: "character varying(450)",
					maxLength: 450,
					nullable: false
				),
				AggregateType = table.Column<string>(
					type: "character varying(450)",
					maxLength: 450,
					nullable: false
				),
				Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
				IsDeleted = table.Column<bool>(
					type: "boolean",
					nullable: false,
					defaultValue: false
				),
				Payload = table.Column<string>(type: "jsonb", nullable: true),
				EventType = table.Column<string>(
					type: "character varying(450)",
					maxLength: 450,
					nullable: true
				),
				IdempotencyId = table.Column<string>(
					type: "character varying(450)",
					maxLength: 450,
					nullable: true
				),
				Timestamp = table.Column<DateTimeOffset>(
					type: "timestamp with time zone",
					nullable: false,
					defaultValueSql: "CURRENT_TIMESTAMP"
				),
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_EventStoreEvents", x => x.Id);
			}
		);

		migrationBuilder
			.CreateIndex(
				name: "IX_EventStoreEvents_AggregateId_EntityType",
				schema: "public",
				table: "EventStoreEvents",
				columns: new[] { "AggregateId", "AggregateType", "EntityType" }
			)
			.Annotation("Npgsql:IndexInclude", new[] { "Version", "IsDeleted" });

		migrationBuilder
			.CreateIndex(
				name: "IX_EventStoreEvents_AggregateType_EntityType",
				schema: "public",
				table: "EventStoreEvents",
				columns: new[] { "AggregateType", "EntityType", "IsDeleted" }
			)
			.Annotation("Npgsql:IndexInclude", new[] { "AggregateId" });

		migrationBuilder
			.CreateIndex(
				name: "IX_EventStoreEvents_EventRange",
				schema: "public",
				table: "EventStoreEvents",
				columns: new[] { "AggregateId", "AggregateType", "Version" },
				filter: "\"EntityType\" = 1"
			)
			.Annotation(
				"Npgsql:IndexInclude",
				new[] { "Payload", "EventType", "IdempotencyId", "Timestamp" }
			);
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable(name: "EventStoreEvents", schema: "public");
	}
}
