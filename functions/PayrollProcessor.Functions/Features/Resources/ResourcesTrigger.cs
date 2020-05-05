using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PayrollProcessor.Functions.Features.Employees;
using PayrollProcessor.Functions.Features.Departments;
using PayrollProcessor.Infrastructure.Seeding.Features.Employees;
using PayrollProcessor.Infrastructure.Seeding.Features.Generators;

using static PayrollProcessor.Functions.Infrastructure.AppResources.CosmosDb;

namespace PayrollProcessor.Functions.Features.Resources
{
    public class ResourcesTrigger
    {
        private readonly CosmosClient client;

        public ResourcesTrigger(CosmosClient client) =>
            this.client = client ?? throw new ArgumentNullException(nameof(client));

        [FunctionName(nameof(CreateResources))]
        public async Task<ActionResult> CreateResources(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "resources")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"Creating all tables and queues: [{req}]");

            var dbResponse = await client.CreateDatabaseIfNotExistsAsync(Databases.PayrollProcessor.Name);

            await dbResponse.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(Databases.PayrollProcessor.Containers.Employees, partitionKeyPath: "/partitionKey"));
            await dbResponse.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(Databases.PayrollProcessor.Containers.Departments, partitionKeyPath: "/partitionKey"));

            return new OkResult();
        }

        [FunctionName(nameof(DeleteAllResources))]
        public async Task<ActionResult> DeleteAllResources(
            [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "resources")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"Deleting all tables and queues: [{req}]");

            var db = client.GetDatabase(Databases.PayrollProcessor.Name);

            await db.DeleteAsync();

            return new OkResult();
        }

        [FunctionName(nameof(CreateData))]
        public async Task<ActionResult> CreateData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "resources/data")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"Creating all seed data: [{req}]");

            req.Query.TryGetValue("employeesCount", out var employeesCountQuery);
            req.Query.TryGetValue("payrollsMaxCount", out var payrollsMaxCountQuery);

            int employeesCount = int.Parse(employeesCountQuery.FirstOrDefault() ?? "5");
            int payrollsMaxCount = int.Parse(payrollsMaxCountQuery.FirstOrDefault() ?? "10");

            var domainSeed = new DomainSeed(new EmployeeSeed());

            var employeesContainer = client.GetContainer(Databases.PayrollProcessor.Name, Databases.PayrollProcessor.Containers.Employees);
            var departmentsContainer = client.GetContainer(Databases.PayrollProcessor.Name, Databases.PayrollProcessor.Containers.Departments);

            foreach (var employee in domainSeed.BuildAll(employeesCount, payrollsMaxCount))
            {
                var employeeEntity = EmployeeEntity.Map.From(employee);
                var departmentEmployeeEntity = DepartmentEmployeeEntity.Map.CreateNewFrom(employee);

                await employeesContainer.CreateItemAsync(employeeEntity);
                await departmentsContainer.CreateItemAsync(departmentEmployeeEntity);

                foreach (var payroll in employee.Payrolls)
                {
                    var departmentPayrollEntity = DepartmentPayrollEntity.Map.CreateNewFrom(employee, payroll);
                    var employeePayrollEntity = EmployeePayrollEntity.Map.From(payroll);

                    await departmentsContainer.CreateItemAsync(departmentPayrollEntity);
                    await employeesContainer.CreateItemAsync(employeePayrollEntity);
                }
            }

            return new OkResult();
        }
    }
}
