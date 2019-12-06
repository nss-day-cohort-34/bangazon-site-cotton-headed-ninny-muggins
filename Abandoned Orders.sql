select ProductTypeId, Label, count(OrderId) as [IncompleteOrderCount]
                                        from(select p.ProductTypeId, pt.Label, o.OrderId
                                                from[Order] o
                                                left join OrderProduct op on op.OrderId = o.OrderId
                                                left join Product p on p.ProductId = op.ProductId
                                                left join ProductType pt on pt.ProductTypeId = p.ProductTypeId
                                                where o.DateCompleted IS NULL and p.UserId = '00000000-ffff-ffff-ffff-ffffffffffff'
                                                group by o.OrderId, p.ProductTypeId, pt.Label
                                                     ) o
                                        group by ProductTypeId, Label
                                        order by ProductTypeId