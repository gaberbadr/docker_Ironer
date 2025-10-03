using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Entities;
using CoreLayer.Repositorys_contract;
using CoreLayer.Specifications;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Data.Context;

namespace RepositoryLayer.Repositories
{
    public class GenaricRepository<TEntity, TKey> : IGenaricRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        private readonly AppDbContext _dbContext;
        public GenaricRepository(AppDbContext dBContext)
        {
            _dbContext = dBContext;
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {

            return await _dbContext.Set<TEntity>().ToListAsync();
        }

        public async Task<TEntity> GetAsync(TKey id)
        {

            return await _dbContext.Set<TEntity>().FindAsync(id);
        }


        public async Task AddAsync(TEntity entity)
        {
            await _dbContext.Set<TEntity>().AddAsync(entity);
        }

        public void Update(TEntity entity)
        {
            _dbContext.Set<TEntity>().Update(entity);
        }
        public void Delete(TEntity entity)
        {
            _dbContext.Set<TEntity>().Remove(entity);
        }

        //Refactory function
        private IQueryable<TEntity> ApplySpecfications(ISpecifications<TEntity, TKey> spec)
        {
            return SpecificationsEvaluator<TEntity, TKey>.GetQuery(_dbContext.Set<TEntity>(), spec);
        }

        public async Task<int> GetCountAsync(ISpecifications<TEntity, TKey> spec)
        {
            return await ApplySpecfications(spec).CountAsync();
            // return await SpecificationsEvaluator<TEntity, TKey>.GetQuery(_dbContext.Set<TEntity>(), spec).CountAsync();
        }
        public async Task<IEnumerable<TEntity>> GetAllWithSpecficationAsync(ISpecifications<TEntity, TKey> spec)
        {
            return await ApplySpecfications(spec).ToListAsync();
        }

        public async Task<TEntity> GetWithSpecficationAsync(ISpecifications<TEntity, TKey> spec)
        {
            return await ApplySpecfications(spec).FirstOrDefaultAsync();
        }

    }
}
