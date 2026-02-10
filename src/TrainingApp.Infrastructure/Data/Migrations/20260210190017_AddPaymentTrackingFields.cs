using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "user_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "payment_failed_at",
                table: "user_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_user_subscriptions_stripe_customer_id",
                table: "user_subscriptions",
                column: "stripe_customer_id",
                filter: "stripe_customer_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "i_x_user_subscriptions_stripe_subscription_id",
                table: "user_subscriptions",
                column: "stripe_subscription_id",
                filter: "stripe_subscription_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "i_x_user_subscriptions_stripe_customer_id",
                table: "user_subscriptions");

            migrationBuilder.DropIndex(
                name: "i_x_user_subscriptions_stripe_subscription_id",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "payment_failed_at",
                table: "user_subscriptions");
        }
    }
}
