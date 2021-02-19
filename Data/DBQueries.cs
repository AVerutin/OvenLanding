using System;

namespace OvenLanding.Data
{
    public class DBQueries
    {
        // Запрос всех ЕУ по номеру плавки и последний ТУ, до куда дошла каждая штука
        private string _ingotsMyMeltQuery =
            "set session statement_timeout  to '{0}ms'; " +
            "WITH t0 as" +
            "(select r.unit_id_child as unit_id,r1.unit_id_parent as id_posad,value_s as melt, " +
            "t.unit_id as unit_mes, t.pos " +
            "from mts.unit_tasks t " +
            "join mts.units_relations r on r.unit_id_parent = t.unit_id " +
            "join mts.units_relations r1 on r1.unit_id_child = r.unit_id_parent " +
            "join mts.passport pm on pm.unit_id = r1.unit_id_parent and pm.param_id=10000001 and value_s ='{1}' " +
            "where t.node_id = 20100), " +
            "t1 as (select distinct l.unit_id, " +
            "first_value(l.id) over (partition by l.unit_id order by l.tm_end desc, l.id desc) as id_end " +
            "from mts.locations l where l.unit_id in (select unit_id from t0)), " +
            "t2 as (select t0.unit_id, p.param_id,p.value_s from mts.passport p join t0 on p.unit_id = t0.unit_mes), " +
            "t3 as (select t0.unit_id, p.param_id,p.value_s from mts.passport p join t0 on p.unit_id = t0.id_posad) " +
            "SELECT  l.node_id, n.node_code, l.unit_id, l.tm_beg, l.tm_end, " +
            "pw.value_s billet_weight, t0.pos, t0.melt, " +
            "max(case when t2.param_id = 10000016 then t2.value_s end) coil_num, " +
            "max(case when t2.param_id = 10000014 then t2.value_s end) coil_weight, " +
            "max(case when t3.param_id = 10000003 then t3.value_s end) ingot_profile, " +
            "max(case when t3.param_id = 10000002 then t3.value_s end) steel_grade, " +
            "max(case when t3.param_id = 10000018 then t3.value_s end) profile, " +
            "max(case when t3.param_id = 10000010 then t3.value_s end) diameter, " +
            "max(case when t3.param_id = 10000004 then t3.value_s end) count, " +
            "max(case when t3.param_id = 10000005 then t3.value_s end) weight_all " +
            "FROM t0 join mts.locations l on l.unit_id = t0.unit_id " +
            "join t1 on t1.id_end = l.id " +
            "join mts.nodes n on n.id = l.node_id " +
            "left join t2 on t2.unit_id = t0.unit_id " +
            "left join mts.passport pw on pw.unit_id = t0.unit_id and pw.param_id = 1000 " +
            "join t3 on t3.unit_id = t0.unit_id " +
            "GROUP BY l.unit_id, l.tm_beg, l.tm_end, t0.pos, pw.value_s, l.node_id, n.node_code, t0.melt, l.id " +
            "ORDER BY l.id;";

        private string _returnsByMelt =
            "set session statement_timeout  to '{0}ms'; " +
            "with t0 as (select rp.unit_id_parent as id_posad, pr.value_s as prod, " +
            "l.tm_beg::timestamp as tm_beg, l.tm_end::timestamp as tm_end, " +
            "l.unit_id as unit_dt, t.unit_id as unit_task, pm.value_s as melt, " +
            "t.pos, pc.value_s as count, t.date_reg::timestamp as date_reg, " +
            "p.value_n as billet_w, pw.value_n as coil_w, " +
            "l.node_id, n.node_code, l.id " +
            "from mts.locations l join mts.nodes n on n.id = l.node_id " +
            "join mts.units u on u.id= l.unit_id " +
            "left join mts.units_relations r on r.unit_id_child = u.id " +
            "left join mts.unit_tasks t on t.unit_id = r.unit_id_parent and t.node_id = 20100 " +
            "left join mts.units_relations rp on rp.unit_id_child = r.unit_id_parent " +
            "left join mts.passport pm on pm.unit_id = rp.unit_id_parent and pm.param_id=10000001 " +
            "left join mts.passport pc on pc.unit_id = rp.unit_id_parent and pc.param_id=10000004 " +
            "left join mts.passport pr on pr.unit_id = rp.unit_id_parent and pr.param_id=1 " +
            "left join mts.passport p on p.unit_id = l.unit_id and p.param_id=1000 " +
            "left join mts.passport pw on pw.unit_id = t.unit_id and pw.param_id=10000014 " +
            "where l.node_id = 50100 and pm.value_s='{1}' order by node_id, l.tm_beg) " +
            "select melt, tm_beg, tm_end, pos, count, date_reg, billet_w, node_code, node_id " +
            "from t0 group by melt, tm_beg, tm_end, pos, count, date_reg, billet_w, node_code, node_id order by tm_beg;";


        private string _returnsByPeriod =
            "set session statement_timeout  to '{0}ms'; " +
            "with t0 as (select rp.unit_id_parent as id_posad, pr.value_s as prod, " +
            "l.tm_beg::timestamp as tm_beg, l.tm_end::timestamp as tm_end, " +
            "l.unit_id as unit_dt, t.unit_id as unit_task, pm.value_s as melt, t.pos, " +
            "pc.value_s as count, t.date_reg::timestamp as date_reg, " +
            "p.value_n as billet_w, pw.value_n as coil_w, l.node_id, n.node_code, l.id " +
            "from mts.locations l join mts.nodes n on n.id = l.node_id " +
            "join mts.units u on u.id= l.unit_id " +
            "left join mts.units_relations r on r.unit_id_child = u.id " +
            "left join mts.unit_tasks t on t.unit_id = r.unit_id_parent and t.node_id = 20100 " +
            "left join mts.units_relations rp on rp.unit_id_child = r.unit_id_parent " +
            "left join mts.passport pm on pm.unit_id = rp.unit_id_parent and pm.param_id=10000001 " +
            "left join mts.passport pc on pc.unit_id = rp.unit_id_parent and pc.param_id=10000004 " +
            "left join mts.passport pr on pr.unit_id = rp.unit_id_parent and pr.param_id=1 " +
            "left join mts.passport p on p.unit_id = l.unit_id and p.param_id=1000 " +
            "left join mts.passport pw on pw.unit_id = t.unit_id and pw.param_id=10000014 " +
            "where l.node_id = 50100 and l.tm_beg between '{1}'::timestamp and '{2}'::timestamp " +
            "order by node_id, l.tm_beg) " +
            "select melt, tm_beg, tm_end, pos, count, date_reg, billet_w, node_code, node_id from t0 " +
            "group by melt, tm_beg, tm_end, pos, count, date_reg, billet_w, node_code, node_id order by tm_beg; ";

        /// <summary>
        /// Получить запрос на список ЕУ по номеру плавки и последний ТУ, куда дошла ЕУ
        /// </summary>
        /// <param name="melt">Номер плавки</param>
        /// <param name="timeout">Максимальное время выполнения запроса в мс</param>
        /// <returns>Запрос на список ЕУ по номеру плавки и последний ТУ, куда дошла ЕУ</returns>
        public string GetIngotsMyMeltQuery(string melt, int timeout)
        {
            return string.Format(_ingotsMyMeltQuery, timeout, melt);
        }

        /// <summary>
        /// Получить запрос на получение списка возвратов по номеру плавки
        /// </summary>
        /// <param name="melt">Номер плавки</param>
        /// <param name="timeout">Максимальное время выполнения запроса в мс</param>
        /// <returns>Запрос на получение списка возвратов по номеру плавки</returns>
        public string GetReturnsByMelt(string melt, int timeout)
        {
            return string.Format(_returnsByMelt, timeout, melt);
        }

        /// <summary>
        /// Получить запрос на получение списка возвратов за период
        /// </summary>
        /// <param name="begin">Начало периода</param>
        /// <param name="end">Конец периода</param>
        /// <param name="timeout">Максимальное время выполнения запроса в мс</param>
        /// <returns>Запрос на получение списка возвратов за период</returns>
        public string GetReturnsByPeriod(DateTime begin, DateTime end, int timeout)
        {
            return string.Format(_returnsByPeriod, timeout, begin, end);
        }
    }
}