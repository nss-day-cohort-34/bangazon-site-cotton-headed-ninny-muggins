select ProductTypeId, Label, count(OrderId) as [IncompleteOrderCount]
                                        from(select p.ProductTypeId, pt.Label, o.OrderId
                                                from[Order] o
                                                inner join OrderProduct op on op.OrderId = o.OrderId
                                                inner join Product p on p.ProductId = op.ProductId
                                                inner join ProductType pt on pt.ProductTypeId = p.ProductTypeId
                                                where o.PaymentTypeId IS NULL and p.UserId = '00000000-ffff-ffff-ffff-ffffffffffff'
                                                group by o.OrderId, p.ProductTypeId, pt.Label
                                                     ) oo
                                        group by ProductTypeId, Label
                                        order by ProductTypeId