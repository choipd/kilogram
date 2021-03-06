﻿using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Telegram.Core.Logging;
using Telegram.MTProto;
using Telegram.Utils;

namespace Telegram.Model.Wrappers {
    public class MessageModelDelivered : MessageModel {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(MessageModelDelivered));

        private Message message;

        public MessageModelDelivered(Message message) {
            this.message = message;
        }

        public MessageModelDelivered() {
           
        }

        public MessageModelDelivered(BinaryReader reader) {
            Read(reader);
        }

        public override int Id {
            get {
                switch (message.Constructor) {
                    case Constructor.message:
                        return ((MessageConstructor)message).id;
                    case Constructor.messageForwarded:
                        return ((MessageForwardedConstructor)message).id;
                    case Constructor.messageService:
                        return ((MessageServiceConstructor)message).id;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public override bool IsForwarded {
            get {
                return message.Constructor == Constructor.messageForwarded;
            }
        }

        public override bool IsContact {
            get {
                if (message.Constructor != Constructor.message)
                    return false;

                MessageMedia media = ((MessageConstructor)message).media;
                return media.Constructor == Constructor.messageMediaContact;
            }
        }

        public override string ContactName {
            get {
                if (message.Constructor != Constructor.message)
                    return "";

                MessageMedia media = ((MessageConstructor)message).media;

                if (media.Constructor != Constructor.messageMediaContact)
                    return "";

                MessageMediaContactConstructor mediaContact = (MessageMediaContactConstructor) media;
                return mediaContact.first_name + " " + mediaContact.last_name;
            }
        }

        public override int ForwardedId {
            get {
                if (message.Constructor == Constructor.messageForwarded)
                    return ((MessageForwardedConstructor) message).fwd_from_id;

                return 0;
            }
        }

        public override string ForwardedFrom {
            get {
                if (!IsForwarded)
                    return null;

                MessageForwardedConstructor msgCons = (MessageForwardedConstructor) message;
                return "From " + TelegramSession.Instance.GetUser(msgCons.fwd_from_id).FullName;
            }
        }

        public override bool IsOut {
            get {
                int myId = TelegramSession.Instance.SelfId;
                switch (message.Constructor) {
                    case Constructor.message:
                        return ((MessageConstructor)message).from_id == myId;
                    case Constructor.messageForwarded:
                        return ((MessageForwardedConstructor)message).from_id == myId;
                    case Constructor.messageService:
                        return ((MessageServiceConstructor)message).from_id == myId;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public override bool IsService {
            get {
                return message.Constructor == Constructor.messageService;
            }
        }

        public Message RawMessage {
            get {
                return message;
            }
        }

        public override bool Delivered {
            get {
                return true;
            }
        }

        public override string Text {
            get {
                switch (message.Constructor) {
                    case Constructor.message:
                        return ((MessageConstructor)message).message;
                    case Constructor.messageForwarded:
                        return ((MessageForwardedConstructor)message).message;
                    case Constructor.messageService:
                        return GetServiceString((MessageServiceConstructor) message);
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
            set { }
        }

        private string GetServiceString(MessageServiceConstructor message) {
            UserModel from = TelegramSession.Instance.GetUser(message.from_id);
            return from.FullName + " " + Preview;
        }

        // NOTE: lower case
        public override string Preview {
            get {

                if (message.Constructor == Constructor.message) {
                    
                    string preview = ((MessageConstructor)message).message;
                    MessageMedia media = ((MessageConstructor) message).media;

                    if (media.Constructor == Constructor.messageMediaAudio) {
                        preview = "audio";
                    } else if (media.Constructor == Constructor.messageMediaContact) {
                        preview = "contact";
                    } else if (media.Constructor == Constructor.messageMediaPhoto) {
                        preview = "photo";
                    } else if (media.Constructor == Constructor.messageMediaGeo) {
                        preview = "location";
                    } else if (media.Constructor == Constructor.messageMediaVideo) {
                        preview = "video";
                    } else if (media.Constructor == Constructor.messageMediaDocument) {
                        preview = "document";
                    } else if (media.Constructor == Constructor.messageMediaUnsupported) {
                        preview = "media";
                    }

                    return preview;
                } else if (message.Constructor == Constructor.messageForwarded) {
                    return "forwarded message";
                } else if (message.Constructor == Constructor.messageService) {
                    MessageAction action = ((MessageServiceConstructor)message).action;

                    if (action.Constructor == Constructor.messageActionChatAddUser) {
                        return "added user";
                    } else if (action.Constructor == Constructor.messageActionChatCreate) {
                        return "created chat";
                    } else if (action.Constructor == Constructor.messageActionChatDeletePhoto) {
                        return "removed chat photo";
                    } else if (action.Constructor == Constructor.messageActionChatEditPhoto) {
                        return "changed chat photo";
                    } else if (action.Constructor == Constructor.messageActionChatDeleteUser) {
                        return "removed user from chat";
                    } else if (action.Constructor == Constructor.messageActionChatEditTitle) {
                        return "changed chat title";
                    } else if (action.Constructor == Constructor.messageActionGeoChatCheckin) {
                        return "checked in geo chat";
                    } else if (action.Constructor == Constructor.messageActionGeoChatCreate) {
                        return "created geo chat";
                    } else if (action.Constructor == Constructor.messageActionEmpty) {
                        return "empty";
                    }
                }

                return "";
            }
        }

        public override bool Unread {
            get {
                switch (message.Constructor) {
                    case Constructor.message:
                        return ((MessageConstructor)message).unread;
                    case Constructor.messageForwarded:
                        return ((MessageForwardedConstructor)message).unread;
                    case Constructor.messageService:
                        return ((MessageServiceConstructor)message).unread;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public override void MarkRead() {
            switch (message.Constructor) {
                case Constructor.message:
                    ((MessageConstructor) message).unread = false;
                    break;
                case Constructor.messageForwarded:
                    ((MessageForwardedConstructor) message).unread = false;
                    break;
                case Constructor.messageService:
                    ((MessageServiceConstructor)message).unread = false;
                    break;
            }
        }

        public override DateTime Timestamp {
            get {
                return DateTimeExtensions.DateTimeFromUnixTimestampSeconds(UnixSecondsTime);
            }
            set { }
        }

        public override string TimeString {
            get {
                return Formatters.FormatDialogDateTimestampUnix(UnixSecondsTime);
            }
        }

        public int UnixSecondsTime {
            get {
                int unixSeconds = 0;
                switch (message.Constructor) {
                    case Constructor.message:
                        unixSeconds = ((MessageConstructor)message).date;
                        break;
                    case Constructor.messageForwarded:
                        unixSeconds = ((MessageForwardedConstructor)message).date;
                        break;
                    case Constructor.messageService:
                        unixSeconds = ((MessageServiceConstructor)message).date;
                        break;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }

                return unixSeconds;
            }
        }

        public Peer Peer {
            get {
                if (message.Constructor == Constructor.message) {
                    MessageConstructor msg = (MessageConstructor)message;
                    if (msg.to_id.Constructor == Constructor.peerChat) {
                        return msg.to_id;
                    } else {
                        if (msg.output) {
                            return msg.to_id;
                        } else {
                            return TL.peerUser(msg.from_id);
                        }
                    }
                } else if (message.Constructor == Constructor.messageForwarded) {
                    MessageForwardedConstructor msg = (MessageForwardedConstructor)message;
                    if (msg.to_id.Constructor == Constructor.peerChat) {
                        return msg.to_id;
                    } else {
                        if (msg.output) {
                            return msg.to_id;
                        } else {
                            return TL.peerUser(msg.from_id);
                        }
                    }
                } else if (message.Constructor == Constructor.messageService) {
                    MessageServiceConstructor msg = (MessageServiceConstructor)message;
                    if (msg.to_id.Constructor == Constructor.peerChat) {
                        return msg.to_id;
                    } else {
                        if (msg.output) {
                            return msg.to_id;
                        } else {
                            return TL.peerUser(msg.from_id);
                        }
                    }
                } else {
                    throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public override bool IsChat {
            get {
                return Peer.Constructor == Constructor.peerChat;
            }
    }
        private static Peer GetPeer(MessageConstructor message) {
            return null;
        }

        //private static Peer 

        public override void Write(BinaryWriter writer) {
            writer.Write(1);
            message.Write(writer);
        }

        public override MessageDeliveryState GetMessageDeliveryState() {

            if (message.Constructor == Constructor.message) {
                MessageConstructor messageConstructor = (MessageConstructor)message;
                return messageConstructor.unread ? MessageDeliveryState.Delivered : MessageDeliveryState.Read;
            } else if (message.Constructor == Constructor.messageForwarded) {
                return ((MessageForwardedConstructor)message).unread ? MessageDeliveryState.Delivered : MessageDeliveryState.Read;
            } else if (message.Constructor == Constructor.messageService) {
                return ((MessageServiceConstructor)message).unread ? MessageDeliveryState.Delivered : MessageDeliveryState.Read;
            }
            
            return MessageDeliveryState.Delivered;
        }

        public override UserModel Sender {
            get {
                switch (message.Constructor) {
                    case Constructor.message:
                        return TelegramSession.Instance.GetUser(((MessageConstructor)message).from_id);
                        break;
                    case Constructor.messageForwarded:
                        return TelegramSession.Instance.GetUser(((MessageForwardedConstructor)message).from_id);
                        break;
                    case Constructor.messageService:
                        return TelegramSession.Instance.GetUser(((MessageServiceConstructor)message).from_id);
                        break;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public void SetReadState() {
            if (message.Constructor == Constructor.message) {
                MessageConstructor messageConstructor = (MessageConstructor)message;
                messageConstructor.unread = false;
            } else if (message.Constructor == Constructor.messageForwarded) {
                ((MessageForwardedConstructor) message).unread = false;
            } else if (message.Constructor == Constructor.messageService) {
                ((MessageServiceConstructor) message).unread = false;
            }

            OnPropertyChanged("MessageDeliveryStateProperty");
        }

        public override MessageDeliveryState MessageDeliveryStateProperty {
            get {
                return GetMessageDeliveryState();
            }
        }

        // may be null 
        public MessageMedia MessageMedia {
            get {
                if (message.Constructor != Constructor.message)
                    return null;

                return ((MessageConstructor) message).media;
            }
        }

        private BitmapImage locationBm;
        private static int mapSize = 300;
        // it's OK to return a null, will be handled
        public override BitmapImage Attachment {
            get {
                if (message.Constructor == Constructor.messageService)
                    return null;

                // media is cached
                if (_previewPath != null) {
                    return Utils.Helpers.GetBitmapImageInternal(_previewPath);
                }

                // geo is cached
                if (locationBm != null)
                    return locationBm;

                MessageMedia media = null;

                switch (message.Constructor) {
                    case Constructor.message:
                        media = ((MessageConstructor)message).media;
                        break;
                    case Constructor.messageForwarded:
                        media = ((MessageForwardedConstructor)message).media;
                        break;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }

                // no media found
                if (media == null)
                    return null;


                FileLocation resultFileLocation = null;

                if (media.Constructor == Constructor.messageMediaPhoto) {
                    Photo photo = ((MessageMediaPhotoConstructor) media).photo;
                    
                    if (photo.Constructor == Constructor.photoEmpty)
                        return null;

                    PhotoConstructor photoConstructor = (PhotoConstructor) photo;
                    resultFileLocation = Helpers.GetPreviewFileLocation(photoConstructor);
                } else if (media.Constructor == Constructor.messageMediaVideo) {
                    Video video = ((MessageMediaVideoConstructor) media).video;

                    if (video.Constructor == Constructor.videoEmpty)
                        return null;

                    VideoConstructor videoConstructor = (VideoConstructor) video;
                    resultFileLocation = Helpers.FileLocationGetVideoThumbLocation(videoConstructor);
                } else if (media.Constructor == Constructor.messageMediaGeo) {
                    MessageMediaGeoConstructor cons = (MessageMediaGeoConstructor) media;
                    GeoPoint geoPoint = cons.geo;

                    if (geoPoint.Constructor == Constructor.geoPointEmpty)
                        return null;

                    GeoPointConstructor geoPointConstructor = (GeoPointConstructor) geoPoint;


                    locationBm = new BitmapImage();

                    locationBm.UriSource = new Uri("https://maps.googleapis.com/maps/api/staticmap?center=" + geoPointConstructor.lat + "," + geoPointConstructor.lng.ToString().Replace(",", ".") + "&zoom=12&size=" + mapSize + "x" + mapSize + "&sensor=false&markers=color:red|" + geoPointConstructor.lat.ToString().Replace(",", ".") + "," + geoPointConstructor.lng.ToString().Replace(",", "."));

                    locationBm.ImageOpened += delegate {
                        OnPropertyChanged("Attachment");
                    };

                    return new BitmapImage(new Uri("/Assets/UI/black-placeholder.png", UriKind.Relative));
                }

                if (resultFileLocation == null)
                    return null;

                Task<string> getFileTask = TelegramSession.Instance.Files.GetAvatar(resultFileLocation);
                if (getFileTask.IsCompleted) {
                    _previewPath = getFileTask.Result;
                    return Utils.Helpers.GetBitmapImageInternal(_previewPath);
                }

                getFileTask.ContinueWith((path) => SetPreviewPath(path.Result), TaskScheduler.FromCurrentSynchronizationContext());

                return new BitmapImage(new Uri("/Assets/UI/black-placeholder.png", UriKind.Relative));
            }
        }

        private string _previewPath = null;
        public void SetPreviewPath(string path) {
            _previewPath = path;
            OnPropertyChanged("Attachment");
        }

        private void Read(BinaryReader reader) {
            message = TL.Parse<Message>(reader);
        }
    }
}
