namespace LegacyRenewalApp
{
    public class CustomerRepositoryAdapter : ICustomerRepository
    {
        private readonly CustomerRepository _repository;

        public CustomerRepositoryAdapter(CustomerRepository repository)
        {
            _repository = repository;
        }

        public Customer GetById(int customerId)
        {
            return _repository.GetById(customerId);
        }
    }

    public class SubscriptionPlanRepositoryAdapter : ISubscriptionPlanRepository
    {
        private readonly SubscriptionPlanRepository _repository;

        public SubscriptionPlanRepositoryAdapter(SubscriptionPlanRepository repository)
        {
            _repository = repository;
        }

        public SubscriptionPlan GetByCode(string code)
        {
            return _repository.GetByCode(code);
        }
    }
}