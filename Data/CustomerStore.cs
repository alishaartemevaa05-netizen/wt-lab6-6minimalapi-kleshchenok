using wt_lab6_6minimalapi_kleshchenok.Models;

namespace wt_lab6_6minimalapi_kleshchenok.Data;

public static class CustomerStore
{
    private static readonly object _lock = new();

    public static List<Customer> Customers { get; } = new()
    {
        new Customer { Id = 1, FullName = "Иванов Иван Иванович",
                       Phone = "+375291112233", Email = "ivanov@mail.ru", DriverLicense = "AB123456" },
        new Customer { Id = 2, FullName = "Петрова Анна Сергеевна",
                       Phone = "+375292223344", Email = "petrova@mail.ru", DriverLicense = "CD789012" },
        new Customer { Id = 3, FullName = "Сидоров Пётр Алексеевич",
                       Phone = "+375293334455", Email = null, DriverLicense = "EF345678" },
        new Customer { Id = 4, FullName = "Козлова Мария Андреевна",
                       Phone = "+375294445566", Email = "kozlova@mail.ru", DriverLicense = "GH901234" }
    };

    public static List<Customer> GetAll() { lock (_lock) { return Customers.ToList(); } }

    public static Customer? GetById(int id) { lock (_lock) { return Customers.FirstOrDefault(c => c.Id == id); } }

    public static Customer Add(Customer customer)
    {
        lock (_lock)
        {
            customer.Id = Customers.Count == 0 ? 1 : Customers.Max(x => x.Id) + 1;
            Customers.Add(customer);
            return customer;
        }
    }

    public static Customer? Update(int id, Customer updated)
    {
        lock (_lock)
        {
            var customer = Customers.FirstOrDefault(c => c.Id == id);
            if (customer == null) return null;
            customer.FullName = updated.FullName;
            customer.Phone = updated.Phone;
            customer.Email = updated.Email;
            customer.DriverLicense = updated.DriverLicense;
            return customer;
        }
    }

    public static bool Remove(int id)
    {
        lock (_lock)
        {
            var customer = Customers.FirstOrDefault(c => c.Id == id);
            if (customer == null) return false;
            Customers.Remove(customer);
            return true;
        }
    }
}
