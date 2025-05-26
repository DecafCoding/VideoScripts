using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoScripts.Migrations
{
    /// <inheritdoc />
    public partial class clustertables2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TopicClusterAssignmentEntity_TopicClusterEntity_TopicClusterId",
                table: "TopicClusterAssignmentEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_TopicClusterAssignmentEntity_TranscriptTopics_TranscriptTopicId",
                table: "TopicClusterAssignmentEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_TopicClusterEntity_Projects_ProjectId",
                table: "TopicClusterEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TopicClusterEntity",
                table: "TopicClusterEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TopicClusterAssignmentEntity",
                table: "TopicClusterAssignmentEntity");

            migrationBuilder.RenameTable(
                name: "TopicClusterEntity",
                newName: "TopicClusters");

            migrationBuilder.RenameTable(
                name: "TopicClusterAssignmentEntity",
                newName: "TopicClusterAssignments");

            migrationBuilder.RenameIndex(
                name: "IX_TopicClusterEntity_ProjectId",
                table: "TopicClusters",
                newName: "IX_TopicClusters_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_TopicClusterAssignmentEntity_TranscriptTopicId",
                table: "TopicClusterAssignments",
                newName: "IX_TopicClusterAssignments_TranscriptTopicId");

            migrationBuilder.RenameIndex(
                name: "IX_TopicClusterAssignmentEntity_TopicClusterId",
                table: "TopicClusterAssignments",
                newName: "IX_TopicClusterAssignments_TopicClusterId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopicClusters",
                table: "TopicClusters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopicClusterAssignments",
                table: "TopicClusterAssignments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TopicClusters_ClusterName",
                table: "TopicClusters",
                column: "ClusterName");

            migrationBuilder.CreateIndex(
                name: "IX_TopicClusters_DisplayOrder",
                table: "TopicClusters",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TopicClusters_Project_DisplayOrder",
                table: "TopicClusters",
                columns: new[] { "ProjectId", "DisplayOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_TopicClusterAssignments_TopicClusters_TopicClusterId",
                table: "TopicClusterAssignments",
                column: "TopicClusterId",
                principalTable: "TopicClusters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TopicClusterAssignments_TranscriptTopics_TranscriptTopicId",
                table: "TopicClusterAssignments",
                column: "TranscriptTopicId",
                principalTable: "TranscriptTopics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TopicClusters_Projects_ProjectId",
                table: "TopicClusters",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TopicClusterAssignments_TopicClusters_TopicClusterId",
                table: "TopicClusterAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TopicClusterAssignments_TranscriptTopics_TranscriptTopicId",
                table: "TopicClusterAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TopicClusters_Projects_ProjectId",
                table: "TopicClusters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TopicClusters",
                table: "TopicClusters");

            migrationBuilder.DropIndex(
                name: "IX_TopicClusters_ClusterName",
                table: "TopicClusters");

            migrationBuilder.DropIndex(
                name: "IX_TopicClusters_DisplayOrder",
                table: "TopicClusters");

            migrationBuilder.DropIndex(
                name: "IX_TopicClusters_Project_DisplayOrder",
                table: "TopicClusters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TopicClusterAssignments",
                table: "TopicClusterAssignments");

            migrationBuilder.RenameTable(
                name: "TopicClusters",
                newName: "TopicClusterEntity");

            migrationBuilder.RenameTable(
                name: "TopicClusterAssignments",
                newName: "TopicClusterAssignmentEntity");

            migrationBuilder.RenameIndex(
                name: "IX_TopicClusters_ProjectId",
                table: "TopicClusterEntity",
                newName: "IX_TopicClusterEntity_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_TopicClusterAssignments_TranscriptTopicId",
                table: "TopicClusterAssignmentEntity",
                newName: "IX_TopicClusterAssignmentEntity_TranscriptTopicId");

            migrationBuilder.RenameIndex(
                name: "IX_TopicClusterAssignments_TopicClusterId",
                table: "TopicClusterAssignmentEntity",
                newName: "IX_TopicClusterAssignmentEntity_TopicClusterId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopicClusterEntity",
                table: "TopicClusterEntity",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopicClusterAssignmentEntity",
                table: "TopicClusterAssignmentEntity",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TopicClusterAssignmentEntity_TopicClusterEntity_TopicClusterId",
                table: "TopicClusterAssignmentEntity",
                column: "TopicClusterId",
                principalTable: "TopicClusterEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TopicClusterAssignmentEntity_TranscriptTopics_TranscriptTopicId",
                table: "TopicClusterAssignmentEntity",
                column: "TranscriptTopicId",
                principalTable: "TranscriptTopics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TopicClusterEntity_Projects_ProjectId",
                table: "TopicClusterEntity",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
