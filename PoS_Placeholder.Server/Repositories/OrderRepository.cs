﻿using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Repositories;

public class OrderRepository : Repository<Order>
{
    public OrderRepository(ApplicationDbContext db) : base(db)
    {
    }

    public async Task<IEnumerable<Order>> GetOrdersByBusinessIdAsync(int businessId)
    {
        return await _db.Orders
            .Include(o => o.Products)
            .Include(o => o.Taxes)
            .Include(o => o.Discounts)
            .Include(o => o.Services)
            .Where(o => o.BusinessId == businessId)
            .ToListAsync();
    }

    public async Task<Order> GetOrderByOrderIdAndBusinessIdAsync(int orderId, int businessId)
    {
        return await _db.Orders
            .Include(o => o.Products)
            .Include(o => o.Taxes)
            .Include(o => o.Discounts)
            .Include(o => o.Services)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.BusinessId == businessId);
    }

    public async Task<Order> GetOrderAndPaymentsByUserIdAndBID(int userId, int businessId)
    {
        return await _db.Orders
            .Include(o => o.Payments)
            .Include(o => o.Products)
            .Include(o => o.Services)
            .FirstOrDefaultAsync(o => o.Id == userId && o.BusinessId == businessId);
    }
}