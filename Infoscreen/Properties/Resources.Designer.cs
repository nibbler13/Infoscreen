﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Infoscreen.Properties {
    using System;
    
    
    /// <summary>
    ///   Класс ресурса со строгой типизацией для поиска локализованных строк и т.д.
    /// </summary>
    // Этот класс создан автоматически классом StronglyTypedResourceBuilder
    // с помощью такого средства, как ResGen или Visual Studio.
    // Чтобы добавить или удалить член, измените файл .ResX и снова запустите ResGen
    // с параметром /str или перестройте свой проект VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Возвращает кэшированный экземпляр ResourceManager, использованный этим классом.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Infoscreen.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Перезаписывает свойство CurrentUICulture текущего потока для всех
        ///   обращений к ресурсу с помощью этого класса ресурса со строгой типизацией.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на /*changelog:
        ///ver 1.00
        ///1)  Исправлено: запрос возвращал ошибку в случае, если врач работает до 0:00
        ///2)  Добавлен статус 21 &quot;Идет прием&quot; для устранения случаев, когда запрос возвращал несколько строк со статусом 20. Это происходило, если врач вручную снимал
        ///    отметку о завершении приема с последующих по времени приемов.
        ///ver 0.03
        ///1)            Исправлена ошибка при которой в некоторых ситуациях не отображалась инфо о докторе
        ///2)            Должность врача в настоящей версии берется из справочника
        ///3)   [остаток строки не уместился]&quot;;.
        /// </summary>
        internal static string SqlQueryMain {
            get {
                return ResourceManager.GetString("SqlQueryMain", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на -- ver 1.0
        ///select depname,fullname,d0,d1,d2,d3,d4,d5,d6
        ///from
        ///(
        ///select
        ///    upper(dp.depname) depname
        ///  , d.fullname
        ///  , ds.depnum
        ///  , ds.dcode
        ///  , (select lpad(dateadd(minute,min(ds0.beghour*60+ds0.begmin),cast(&apos;00:00&apos; as time)),5)||&apos; - &apos;||lpad(dateadd(minute,max(ds0.endhour*60+ds0.endmin),cast(&apos;00:00&apos; as time)),5) from doctshedule ds0
        ///     where ds0.depnum=ds.depnum and ds0.dcode=ds.dcode and ds0.wdate=dateadd(day,0,current_date)) d0
        ///  , (select lpad(dateadd(minute,min(ds0.beghour*60+ds0.begmin), [остаток строки не уместился]&quot;;.
        /// </summary>
        internal static string SqlQueryTimetable {
            get {
                return ResourceManager.GetString("SqlQueryTimetable", resourceCulture);
            }
        }
    }
}
