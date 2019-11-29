using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Expressions
{
    class Entity
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    class EntityContext : DbContext
    {
        public EntityContext(DbContextOptions<EntityContext> options) : base(options)
        {
        }

        public DbSet<Entity> Entities { get; set; }
    }


    class Program
    {
        private static MethodInfo _orderBy = new Func<IQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.OrderBy).GetMethodInfo().GetGenericMethodDefinition();

        static void Main()
        {
            var options = new DbContextOptionsBuilder<EntityContext>()
                .UseInMemoryDatabase(databaseName: "entityDB")
                .Options;

            // Insert seed data into the database using one instance of the context
            using (var context = new EntityContext(options))
            {
                context.Entities.Add(new Entity() {Name = "test6"});
                context.Entities.Add(new Entity() {Name = "test5"});
                context.Entities.Add(new Entity() {Name = "test4"});
                context.Entities.Add(new Entity() {Name = "Test3"});
                context.Entities.Add(new Entity() {Name = "test1"});
                context.Entities.Add(new Entity() {Name = "test2"});
                context.SaveChanges();
            }

            using (var context = new EntityContext(options))
            {
                var sorted = context.Entities.OrderBy(x=>x.Name).ToList();

                var parameter = Expression.Parameter(typeof(Entity), "x");
                var prop = Expression.Property(parameter, nameof(Entity.Name));

                var toLower = typeof(string).GetMethod("ToLower", System.Type.EmptyTypes);
                var body = Expression.Call(prop, toLower);

                LambdaExpression expression = Expression.Lambda(body, parameter);

                var genericOrderBy = _orderBy.MakeGenericMethod(typeof(Entity), typeof(string));

                var query = context.Entities.AsQueryable();

                var sortedQuery = query.Provider.CreateQuery<Entity>(
                    Expression.Call(
                        (Expression) null,
                        genericOrderBy,
                        query.Expression,
                        (Expression) Expression.Quote((Expression) expression))
                );

                var result = sortedQuery.ToList();
            }
        }
    }
}