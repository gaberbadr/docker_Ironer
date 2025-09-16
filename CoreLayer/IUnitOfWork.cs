using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Entities;
using CoreLayer.Repositorys_contract;

namespace CoreLayer
{
    public interface IUnitOfWork
    {
        Task<int> CompleteAsync();

        //create an function Repository(T)  its return type IGenaricRepository
        IGenaricRepository<TEntity, Tkey> Repository<TEntity, Tkey>() where TEntity : BaseEntity<Tkey>;
    }
}
