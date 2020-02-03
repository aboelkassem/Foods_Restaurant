using Foods.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Foods.Utility
{
    // STATIC DETAILS
    public static class SD
    {
        public const string DefaultFoodImage = "defaultFoodImage.png";

        // Static Roles
        public const string ManagerUser = "Manager";
        public const string KitckenUser = "Kitchen";
        public const string FrontDeskUser = "FrontDesk";
        public const string CustomerEndUser = "Customer";

        public const string ssShoppingCartCount = "ssCartCount";
        public const string ssCouponCode = "ssCouponCode";

        // Order Status
        public const string StatusSubmitted = "Submitted";
        public const string StatusInProcess = "Being Prepared";
        public const string StatusReady = "Ready For Pickup";
        public const string StatusCompleted = "Completed";
        public const string StatusCancelled = "Cancelled";
        // Payment Status
        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusRejected = "Rejected";


        //Status Images
        public const string OrderInKitcken = "InKitchen.png";
        public const string OrderCompleted = "completed.png";
        public const string OrderPlaced = "OrderPlaced.png";
        public const string OrderReadyForPickUp = "ReadyForPickup.png";

        public static string ConvertToRawHtml(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }

        public static double DiscountedPrice(Coupon couponfromDb , double originalOrderTotal)
        {
            if (couponfromDb == null)
            {
                return originalOrderTotal;
            }
            else
            {
                if (couponfromDb.MinimumAmount > originalOrderTotal)
                {
                    // that mean you have no discount because you don't met the minimum amount that can apply for discount
                    return originalOrderTotal;
                }
                else
                {
                    // every thing is valid
                    if (Convert.ToInt32(couponfromDb.CouponType) == (int)Coupon.ECouponType.Dollar)
                    {
                        // $10 off $100 == $90  Just for dollar
                        return Math.Round(originalOrderTotal - couponfromDb.Discount, 2);
                    }

                    if (Convert.ToInt32(couponfromDb.CouponType) == (int)Coupon.ECouponType.Percent)
                    {
                        // 10% off $100 == $90  Just for percent
                        return Math.Round(originalOrderTotal - (originalOrderTotal * couponfromDb.Discount/100), 2);
                    }

                }
            }
            return originalOrderTotal;
        }
    }
}
