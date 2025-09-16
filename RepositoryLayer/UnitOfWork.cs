using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer;
using CoreLayer.Entities;
using CoreLayer.Repositorys_contract;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Data.Context;
using RepositoryLayer.Repositories;

namespace RepositoryLayer
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _StoreDBContext;

        private Hashtable _HashtableRepos;

        public UnitOfWork(AppDbContext storeDBContext)
        {
            _StoreDBContext = storeDBContext;
            _HashtableRepos = new Hashtable();
        }
        public async Task<int> CompleteAsync()
        {
            try
            {
                return await _StoreDBContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("🔥 DB Update Error:");
                Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
                throw; 
            }
        }




        public IGenaricRepository<TEntity, Tkey> Repository<TEntity, Tkey>() where TEntity : BaseEntity<Tkey>
        {


            var type = typeof(TEntity).Name;

            if (!_HashtableRepos.ContainsKey(type))
            {
                var repo = new GenaricRepository<TEntity, Tkey>(_StoreDBContext);
                _HashtableRepos.Add(type, repo);
            }

            return _HashtableRepos[type] as IGenaricRepository<TEntity, Tkey>;
        }
    }
}
