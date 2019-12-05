Insert into [Order] (DateCreated, DateCompleted, UserId, PaymentTypeId) VALUES (GETDATE(), null, '00000000-ffff-ffff-ffff-ffffffffffff', 1)

select * from [Order]
select * from OrderProduct
insert into OrderProduct (OrderId, ProductId) VALUES (5, 1)
insert into OrderProduct (OrderId, ProductId) VALUES (5, 2)
insert into OrderProduct (OrderId, ProductId) VALUES (5, 4)
insert into OrderProduct (OrderId, ProductId) VALUES (5, 7)