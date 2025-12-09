namespace Plurby.Services.Shared
{
    public partial class SharedService
    {
        PlurbyDbContext _dbContext;

        public SharedService(PlurbyDbContext dbContext)
        {
            _dbContext = dbContext;
        }
    }
}
