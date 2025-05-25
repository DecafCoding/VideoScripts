using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoScripts.Migrations
{
    /// <inheritdoc />
    public partial class clustertables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TopicClusterEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClusterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicClusterEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicClusterEntity_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopicClusterAssignmentEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicClusterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TranscriptTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicClusterAssignmentEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicClusterAssignmentEntity_TopicClusterEntity_TopicClusterId",
                        column: x => x.TopicClusterId,
                        principalTable: "TopicClusterEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TopicClusterAssignmentEntity_TranscriptTopics_TranscriptTopicId",
                        column: x => x.TranscriptTopicId,
                        principalTable: "TranscriptTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopicClusterAssignmentEntity_TopicClusterId",
                table: "TopicClusterAssignmentEntity",
                column: "TopicClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicClusterAssignmentEntity_TranscriptTopicId",
                table: "TopicClusterAssignmentEntity",
                column: "TranscriptTopicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopicClusterEntity_ProjectId",
                table: "TopicClusterEntity",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicClusterAssignmentEntity");

            migrationBuilder.DropTable(
                name: "TopicClusterEntity");
        }
    }
}
