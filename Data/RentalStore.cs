using wt_lab6_6minimalapi_kleshchenok.Models;

namespace wt_lab6_6minimalapi_kleshchenok.Data;

public static class RentalStore
{
    private static readonly object _lock = new();

    public static List<Rental> Rentals { get; } = new()
    {
        new Rental { Id = 1, CarId = 1, CustomerId = 1,
                     StartDate = DateTime.Today.AddDays(-10), EndDate = DateTime.Today.AddDays(-5),
                     TotalPrice = 300, Status = RentalStatus.Completed },
        new Rental { Id = 2, CarId = 2, CustomerId = 2,
                     StartDate = DateTime.Today.AddDays(-2), EndDate = DateTime.Today.AddDays(3),
                     TotalPrice = 450, Status = RentalStatus.Active }
    };

    public static List<Rental> GetAll() { lock (_lock) { return Rentals.ToList(); } }

    public static Rental? GetById(int id) { lock (_lock) { return Rentals.FirstOrDefault(r => r.Id == id); } }

    public static Rental Add(Rental rental)
    {
        lock (_lock)
        {
            rental.Id = Rentals.Count == 0 ? 1 : Rentals.Max(x => x.Id) + 1;
            Rentals.Add(rental);
            return rental;
        }
    }

    public static bool HasActiveRental(int carId)
    {
        lock (_lock) { return Rentals.Any(r => r.CarId == carId && r.Status == RentalStatus.Active); }
    }

    public static bool HasActiveRentalForCustomer(int customerId)
    {
        lock (_lock) { return Rentals.Any(r => r.CustomerId == customerId && r.Status == RentalStatus.Active); }
    }

    public static bool Remove(int id)
    {
        lock (_lock)
        {
            var rental = Rentals.FirstOrDefault(r => r.Id == id);
            if (rental == null) return false;
            Rentals.Remove(rental);
            return true;
        }
    }

    public static Rental? Complete(int id)
    {
        lock (_lock)
        {
            var rental = Rentals.FirstOrDefault(r => r.Id == id);
            if (rental != null) rental.Status = RentalStatus.Completed;
            return rental;
        }
    }
}
