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
        ///ver 0.03
        ///1)	Исправлена ошибка при которой в некоторых ситуациях не отображалась инфо о докторе
        ///2)	Должность врача в настоящей версии берется из справочника
        ///3)	Удалён рейтинг врача
        ///ver 0.02
        ///1)	Приглашение на приём - возвращается только имя + отчество
        ///2)	Добавлен рейтинг врача - поле DSINFO 4 колонка
        ///3)	Исправлена ошибка, когда при статусе «Идёт приём» в поле timeleft отображалось 0 мин.
        ///*/ --ver 0.03
        ///WITH a AS (
        ///SELECT chid,
        ///       x.rnum,
        ///       CASE
        ///           WHEN x.starttreat=1 [остаток строки не уместился]&quot;;.
        /// </summary>
        internal static string SqlQueryMain {
            get {
                return ResourceManager.GetString("SqlQueryMain", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на .
        /// </summary>
        internal static string SqlQueryTimetable {
            get {
                return ResourceManager.GetString("SqlQueryTimetable", resourceCulture);
            }
        }
    }
}
