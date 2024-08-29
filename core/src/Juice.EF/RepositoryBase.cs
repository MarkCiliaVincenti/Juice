﻿using Juice.Domain;
using Microsoft.EntityFrameworkCore;

namespace Juice.EF
{
    public abstract class RepositoryBase<T, TContext> : IRepository<T>
        where T : class
        where TContext : DbContext, IUnitOfWork
    {
        public IUnitOfWork UnitOfWork { get; private set; }
        protected TContext Context => (TContext)UnitOfWork;
        public RepositoryBase(TContext context) => UnitOfWork = context;
    }
}
