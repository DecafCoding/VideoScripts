﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VideoScripts.Data;

#nullable disable

namespace VideoScripts.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250526015203_clustertables2")]
    partial class clustertables2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("VideoScripts.Data.Entities.ChannelEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(5000)
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<DateTime?>("LastCheckDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("LastModifiedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("LastModifiedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("PublishedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("SubscriberCount")
                        .HasColumnType("int");

                    b.Property<string>("ThumbnailURL")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("VideoCount")
                        .HasColumnType("int");

                    b.Property<string>("YTId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("LastCheckDate")
                        .HasDatabaseName("IX_Channels_LastCheckDate");

                    b.HasIndex("Title")
                        .HasDatabaseName("IX_Channels_Title");

                    b.HasIndex("YTId")
                        .IsUnique()
                        .HasDatabaseName("IX_Channels_YTId");

                    b.ToTable("Channels");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.ProjectEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<DateTime>("LastModifiedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("LastModifiedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Topic")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .HasDatabaseName("IX_Projects_Name");

                    b.HasIndex("Topic")
                        .HasDatabaseName("IX_Projects_Topic");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.ScriptEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<DateTime>("LastModifiedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("LastModifiedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId")
                        .HasDatabaseName("IX_Scripts_ProjectId");

                    b.HasIndex("Title")
                        .HasDatabaseName("IX_Scripts_Title");

                    b.HasIndex("ProjectId", "Version")
                        .HasDatabaseName("IX_Scripts_Project_Version");

                    b.ToTable("Scripts");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.TopicClusterAssignmentEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<DateTime>("LastModifiedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("LastModifiedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("TopicClusterId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("TranscriptTopicId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("TopicClusterId")
                        .HasDatabaseName("IX_TopicClusterAssignments_TopicClusterId");

                    b.HasIndex("TranscriptTopicId")
                        .IsUnique()
                        .HasDatabaseName("IX_TopicClusterAssignments_TranscriptTopicId");

                    b.ToTable("TopicClusterAssignments");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.TopicClusterEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ClusterName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("DisplayOrder")
                        .HasColumnType("int");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<DateTime>("LastModifiedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("LastModifiedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ClusterName")
                        .HasDatabaseName("IX_TopicClusters_ClusterName");

                    b.HasIndex("DisplayOrder")
                        .HasDatabaseName("IX_TopicClusters_DisplayOrder");

                    b.HasIndex("ProjectId")
                        .HasDatabaseName("IX_TopicClusters_ProjectId");

                    b.HasIndex("ProjectId", "DisplayOrder")
                        .HasDatabaseName("IX_TopicClusters_Project_DisplayOrder");

                    b.ToTable("TopicClusters");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.TranscriptTopicEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("BluePrintElements")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<bool>("IsSelected")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LastModifiedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("LastModifiedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<TimeSpan>("StartTime")
                        .HasColumnType("time");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("TopicSummary")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<Guid>("VideoId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("IsSelected")
                        .HasDatabaseName("IX_TranscriptTopics_IsSelected");

                    b.HasIndex("StartTime")
                        .HasDatabaseName("IX_TranscriptTopics_StartTime");

                    b.HasIndex("Title")
                        .HasDatabaseName("IX_TranscriptTopics_Title");

                    b.HasIndex("VideoId")
                        .HasDatabaseName("IX_TranscriptTopics_VideoId");

                    b.HasIndex("VideoId", "StartTime")
                        .HasDatabaseName("IX_TranscriptTopics_Video_StartTime");

                    b.ToTable("TranscriptTopics");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.VideoEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ChannelId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CommentCount")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(5000)
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Duration")
                        .HasColumnType("int");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<DateTime>("LastModifiedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("LastModifiedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("LikeCount")
                        .HasColumnType("int");

                    b.Property<string>("MainSummary")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<Guid?>("ProjectId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("PublishedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("RawTranscript")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StructuredContent")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("VideoTopic")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("ViewCount")
                        .HasColumnType("int");

                    b.Property<string>("YTId")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId")
                        .HasDatabaseName("IX_Videos_ChannelId");

                    b.HasIndex("ProjectId")
                        .HasDatabaseName("IX_Videos_ProjectId");

                    b.HasIndex("PublishedAt")
                        .HasDatabaseName("IX_Videos_PublishedAt");

                    b.HasIndex("Title")
                        .HasDatabaseName("IX_Videos_Title");

                    b.HasIndex("VideoTopic")
                        .HasDatabaseName("IX_Videos_VideoTopic");

                    b.HasIndex("YTId")
                        .IsUnique()
                        .HasDatabaseName("IX_Videos_YTId");

                    b.HasIndex("ChannelId", "PublishedAt")
                        .HasDatabaseName("IX_Videos_Channel_PublishedAt");

                    b.ToTable("Videos");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.ScriptEntity", b =>
                {
                    b.HasOne("VideoScripts.Data.Entities.ProjectEntity", "Project")
                        .WithMany("Scripts")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.TopicClusterAssignmentEntity", b =>
                {
                    b.HasOne("VideoScripts.Data.Entities.TopicClusterEntity", "TopicCluster")
                        .WithMany("TopicAssignments")
                        .HasForeignKey("TopicClusterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("VideoScripts.Data.Entities.TranscriptTopicEntity", "TranscriptTopic")
                        .WithOne("ClusterAssignment")
                        .HasForeignKey("VideoScripts.Data.Entities.TopicClusterAssignmentEntity", "TranscriptTopicId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TopicCluster");

                    b.Navigation("TranscriptTopic");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.TopicClusterEntity", b =>
                {
                    b.HasOne("VideoScripts.Data.Entities.ProjectEntity", "Project")
                        .WithMany("TopicClusters")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.TranscriptTopicEntity", b =>
                {
                    b.HasOne("VideoScripts.Data.Entities.VideoEntity", "Video")
                        .WithMany("TranscriptTopics")
                        .HasForeignKey("VideoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Video");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.VideoEntity", b =>
                {
                    b.HasOne("VideoScripts.Data.Entities.ChannelEntity", "Channel")
                        .WithMany("Videos")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("VideoScripts.Data.Entities.ProjectEntity", "Project")
                        .WithMany("Videos")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Channel");

                    b.Navigation("Project");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.ChannelEntity", b =>
                {
                    b.Navigation("Videos");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.ProjectEntity", b =>
                {
                    b.Navigation("Scripts");

                    b.Navigation("TopicClusters");

                    b.Navigation("Videos");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.TopicClusterEntity", b =>
                {
                    b.Navigation("TopicAssignments");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.TranscriptTopicEntity", b =>
                {
                    b.Navigation("ClusterAssignment");
                });

            modelBuilder.Entity("VideoScripts.Data.Entities.VideoEntity", b =>
                {
                    b.Navigation("TranscriptTopics");
                });
#pragma warning restore 612, 618
        }
    }
}
