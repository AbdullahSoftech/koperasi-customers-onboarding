using Application.DTOs;
using Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Wrappers;

namespace Application.Queries.Customers;

public record GetCustomerQuery(Guid CustomerId) : IRequest<ApiResponse<CustomerResponse>>;

public class GetCustomerQueryHandler
    : IRequestHandler<GetCustomerQuery, ApiResponse<CustomerResponse>>
{
    private readonly IAppDbContext _context;

    public GetCustomerQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<CustomerResponse>> Handle(
        GetCustomerQuery request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
            return ApiResponse<CustomerResponse>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");

        return ApiResponse<CustomerResponse>.Ok(new CustomerResponse(
            customer.Id,
            customer.PhoneNumber,
            customer.Email,
            customer.FullName,
            customer.Status.ToString(),
            customer.CustomerType.ToString(),
            customer.CreatedAt
        ));
    }
}