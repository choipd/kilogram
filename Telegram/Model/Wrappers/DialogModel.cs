﻿using System.IO;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public class DialogModel {
        private DialogConstructor dialog;
        private IMessageProvider messageProvider;
        private IUserProvider userProvider;
        private IChatProvider chatProvider;

        public DialogModel(Dialog dialog, IMessageProvider messageProvider, IUserProvider userProvider, IChatProvider chatProvider) {
            this.dialog = (DialogConstructor) dialog;
            this.messageProvider = messageProvider;
            this.userProvider = userProvider;
            this.chatProvider = chatProvider;
        }

        public Dialog RawDialog {
            get {
                return dialog;
            }
        }

        public int Id {
            get {
                switch (dialog.peer.Constructor) {
                    case Constructor.peerChat:
                        var peerChat = dialog.peer as PeerChatConstructor;
                        return peerChat.chat_id;

                    case Constructor.peerUser:
                        var peerUser = dialog.peer as PeerUserConstructor;
                        return peerUser.user_id;
                }

                return -1;
            }
        }

        public string Title {
            get {
                string title = "";
                switch (dialog.peer.Constructor) {
                    case Constructor.peerChat:
                        var peerChat = dialog.peer as PeerChatConstructor;
                        // check null
                        var chatModel = chatProvider.GetChat(peerChat.chat_id);
                        title = chatModel.Title;                        
                        break;

                    case Constructor.peerUser:
                        var peerUser = dialog.peer as PeerUserConstructor;
                        var userModel = userProvider.GetUser(peerUser.user_id);
                        title = userModel.FullName;
                        break;
                }

                return title;
            }
        }

        public string Preview {
            get {
                string preview = "";
                var topMessage = messageProvider.GetMessage(dialog.top_message).RawMessage;

                switch (topMessage.Constructor) {
                    case Constructor.message:
                        preview = ((MessageConstructor)topMessage).message;
                        break;
                    case Constructor.messageForwarded:
                        preview = ((MessageForwardedConstructor)topMessage).message;
                        break;
                    case Constructor.messageService:
                        preview = "SERVICE";
                        break;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }

                return preview;
            }
        }

        public string TimeOrDate {
            get {
                var topMessage = messageProvider.GetMessage(dialog.top_message);
                return topMessage.TimeOrDate;
            }
        }

        public bool IsChat {
            get {
                return dialog.peer.Constructor == Constructor.peerChat;
            }
        }

        public string LastActivityUserName {
            get {
                var topMessage = messageProvider.GetMessage(dialog.top_message).RawMessage;
                UserModel user = null;
                switch (topMessage.Constructor) {
                    case Constructor.message:
                        user = userProvider.GetUser(((MessageConstructor)topMessage).from_id);
                        break;
                    case Constructor.messageForwarded:
                        user = userProvider.GetUser(((MessageForwardedConstructor)topMessage).from_id);
                        break;
                    case Constructor.messageService:
                        break;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
                
                string fullName = "service user";
                
                if (user == null) {
                    return fullName;
                }

                return user.FullName;
            }
        }
    }
}
